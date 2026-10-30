using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Core.Messages;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.DeleteVaccination;
using VaccinationCard.Api.Modules.People.Application.UseCases.RegisterVaccination;
using VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.IntegrationTests.Modules.People.Application.UseCases.DeleteVaccination;

public sealed class DeleteVaccinationHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    [Trait("RN", "RN15")]
    public async Task Should_Delete_Only_The_Vaccination_When_It_Is_The_Last_Dose()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = await MakeDeleteVaccinationInputAsync(cancellationToken);

        // Act
        var output = await SendAsync(input, cancellationToken);

        // Assert
        output.ShouldBe(new SuccessMessage("Vacinação excluída com sucesso."));
        var records = await FindRecordsAsync(input.PersonId, cancellationToken);
        records.ShouldHaveSingleItem().DoseNumber.ShouldBe((short)1);
    }

    [Fact]
    public async Task Should_Throw_Not_Found_When_Person_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = await MakeDeleteVaccinationInputAsync(cancellationToken) with { PersonId = Guid.CreateVersion7() };

        // Act
        var act = () => SendAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("person.not_found");
    }

    [Fact]
    public async Task Should_Throw_Not_Found_When_Vaccination_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = await MakeDeleteVaccinationInputAsync(cancellationToken) with { VaccinationId = Guid.CreateVersion7() };

        // Act
        var act = () => SendAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("vaccination.not_found");
    }

    [Fact]
    public async Task Should_Throw_Not_Found_And_Keep_The_Doses_When_Vaccination_Belongs_To_Another_Person()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var other = await SendAsync(new CreatePersonCommand("João da Silva", "11144477735"), cancellationToken);
        var input = await MakeDeleteVaccinationInputAsync(cancellationToken);

        // Act
        var act = () => SendAsync(input with { PersonId = other.Id }, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("vaccination.not_found");
        var records = await FindRecordsAsync(input.PersonId, cancellationToken);
        records.Count.ShouldBe(2);
    }

    // The request loads the later dose from the database, and the failed removal deletes nothing.
    [Fact]
    [Trait("RN", "RN15")]
    public async Task Should_Reject_Removal_And_Keep_The_Doses_When_Vaccination_Is_Not_The_Last_Dose()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = await MakeDeleteVaccinationInputAsync(cancellationToken);
        var stored = await FindRecordsAsync(input.PersonId, cancellationToken);
        var first = stored.Single(x => x.DoseNumber == 1);

        // Act
        var act = () => SendAsync(input with { VaccinationId = first.Id }, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.not_last_dose");
        var records = await FindRecordsAsync(input.PersonId, cancellationToken);
        records.Count.ShouldBe(2);
    }

    // To change a dose, the operator deletes the vaccination and registers another one (PRD F09).
    [Fact]
    public async Task Should_Register_The_Same_Dose_Again_When_It_Was_Deleted()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = await MakeDeleteVaccinationInputAsync(cancellationToken);
        await SendAsync(input, cancellationToken);
        var remaining = (await FindRecordsAsync(input.PersonId, cancellationToken)).ShouldHaveSingleItem();
        var register = new RegisterVaccinationCommand(input.PersonId, remaining.VaccineId, new DoseDto(DoseType.Primary, 2), new DateOnly(2020, 3, 15));

        // Act
        var output = await SendAsync(register, cancellationToken);

        // Assert
        var records = await FindRecordsAsync(input.PersonId, cancellationToken);
        records.Count.ShouldBe(2);
        var record = records.Single(x => x.DoseNumber == 2);
        record.Id.ShouldBe(output.Id);
        record.AppliedOn.ShouldBe(register.AppliedOn);
    }

    private Task<List<VaccinationRecord>> FindRecordsAsync(Guid personId, CancellationToken cancellationToken)
    {
        return ExecuteDbContextAsync(db => db.Set<VaccinationRecord>().Where(x => x.PersonId == personId).ToListAsync(cancellationToken));
    }

    // A person with the first two doses of a vaccine in the database, and the second, the last one, to delete.
    private async Task<DeleteVaccinationCommand> MakeDeleteVaccinationInputAsync(CancellationToken cancellationToken)
    {
        var person = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        await SendAsync(new RegisterVaccinationCommand(person.Id, vaccine.Id, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15)), cancellationToken);
        var second = await SendAsync(new RegisterVaccinationCommand(person.Id, vaccine.Id, new DoseDto(DoseType.Primary, 2), new DateOnly(2020, 2, 15)), cancellationToken);
        return new DeleteVaccinationCommand(person.Id, second.Id);
    }
}

using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Core.Messages;
using VaccinationCard.Api.Modules.People.Application.UseCases.CorrectVaccinationDate;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.RegisterVaccination;
using VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.IntegrationTests.Modules.People.Application.UseCases.CorrectVaccinationDate;

public sealed class CorrectVaccinationDateHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    [Trait("RN", "RN14")]
    public async Task Should_Store_Only_The_New_Date_When_It_Is_After_The_Previous_Dose()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = await MakeCorrectVaccinationDateInputAsync(cancellationToken);
        var before = await FindRecordAsync(input.VaccinationId, cancellationToken);

        // Act
        var output = await SendAsync(input, cancellationToken);

        // Assert
        output.ShouldBe(new SuccessMessage("Data de aplicação corrigida com sucesso."));
        var record = await FindRecordAsync(input.VaccinationId, cancellationToken);
        record.AppliedOn.ShouldBe(input.AppliedOn);
        record.PersonId.ShouldBe(before.PersonId);
        record.VaccineId.ShouldBe(before.VaccineId);
        record.DoseType.ShouldBe(before.DoseType);
        record.DoseNumber.ShouldBe(before.DoseNumber);
        record.CreatedAt.ShouldBe(before.CreatedAt);
    }

    [Fact]
    public async Task Should_Throw_Not_Found_When_Person_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = await MakeCorrectVaccinationDateInputAsync(cancellationToken) with { PersonId = Guid.CreateVersion7() };

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
        var input = await MakeCorrectVaccinationDateInputAsync(cancellationToken) with { VaccinationId = Guid.CreateVersion7() };

        // Act
        var act = () => SendAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("vaccination.not_found");
    }

    [Fact]
    public async Task Should_Throw_Not_Found_When_Vaccination_Belongs_To_Another_Person()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var other = await SendAsync(new CreatePersonCommand("João da Silva", "11144477735"), cancellationToken);
        var input = await MakeCorrectVaccinationDateInputAsync(cancellationToken) with { PersonId = other.Id };

        // Act
        var act = () => SendAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("vaccination.not_found");
    }

    // The request loads the previous dose from the database, and the failed correction writes nothing.
    [Fact]
    [Trait("RN", "RN14")]
    public async Task Should_Reject_Date_And_Keep_The_Stored_One_When_It_Is_Not_After_The_Previous_Dose()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = await MakeCorrectVaccinationDateInputAsync(cancellationToken) with { AppliedOn = new DateOnly(2020, 1, 15) };

        // Act
        var act = () => SendAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.applied_on_out_of_order");
        var record = await FindRecordAsync(input.VaccinationId, cancellationToken);
        record.AppliedOn.ShouldBe(new DateOnly(2020, 2, 15));
    }

    [Fact]
    [Trait("RN", "RN12")]
    public async Task Should_Store_New_Date_When_It_Is_Today_In_Brasilia()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = await MakeCorrectVaccinationDateInputAsync(cancellationToken) with { AppliedOn = ApiFactory.Today };

        // Act
        await SendAsync(input, cancellationToken);

        // Assert
        var record = await FindRecordAsync(input.VaccinationId, cancellationToken);
        record.AppliedOn.ShouldBe(ApiFactory.Today);
    }

    [Fact]
    [Trait("RN", "RN12")]
    public async Task Should_Reject_Date_When_It_Is_After_Today_In_Brasilia()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = await MakeCorrectVaccinationDateInputAsync(cancellationToken) with { AppliedOn = ApiFactory.Today.AddDays(1) };

        // Act
        var act = () => SendAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.applied_on_in_future");
    }

    private Task<VaccinationRecord> FindRecordAsync(Guid id, CancellationToken cancellationToken)
    {
        return ExecuteDbContextAsync(db => db.Set<VaccinationRecord>().SingleAsync(x => x.Id == id, cancellationToken));
    }

    // A person with the first two doses of a vaccine in the database, and a new date for the second, the last one.
    private async Task<CorrectVaccinationDateCommand> MakeCorrectVaccinationDateInputAsync(CancellationToken cancellationToken)
    {
        var person = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        await SendAsync(new RegisterVaccinationCommand(person.Id, vaccine.Id, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15)), cancellationToken);
        var second = await SendAsync(new RegisterVaccinationCommand(person.Id, vaccine.Id, new DoseDto(DoseType.Primary, 2), new DateOnly(2020, 2, 15)), cancellationToken);
        return new CorrectVaccinationDateCommand(person.Id, second.Id, new DateOnly(2020, 2, 20));
    }
}

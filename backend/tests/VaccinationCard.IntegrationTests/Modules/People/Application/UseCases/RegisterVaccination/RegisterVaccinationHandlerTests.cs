using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.RegisterVaccination;
using VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.IntegrationTests.Modules.People.Application.UseCases.RegisterVaccination;

public sealed class RegisterVaccinationHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Should_Store_Vaccination_When_Dose_Is_The_Next_Expected()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = await MakeRegisterVaccinationInputAsync(cancellationToken);

        // Act
        var output = await SendAsync(input, cancellationToken);

        // Assert
        var record = await ExecuteDbContextAsync(db => db.Set<VaccinationRecord>().SingleAsync(x => x.Id == output.Id, cancellationToken));
        record.PersonId.ShouldBe(input.PersonId);
        record.VaccineId.ShouldBe(input.VaccineId);
        record.DoseType.ShouldBe(DoseType.Primary);
        record.DoseNumber.ShouldBe((short)1);
        record.AppliedOn.ShouldBe(input.AppliedOn);
        record.CreatedAt.ShouldNotBe(default);
    }

    // The second request loads the first dose from the database, so the sequence goes on.
    [Fact]
    [Trait("RN", "RN10")]
    public async Task Should_Store_Second_Dose_When_First_Dose_Was_Registered_Before()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var first = await MakeRegisterVaccinationInputAsync(cancellationToken);
        await SendAsync(first, cancellationToken);
        var input = first with { Dose = new DoseDto(DoseType.Primary, 2), AppliedOn = first.AppliedOn.AddMonths(1) };

        // Act
        await SendAsync(input, cancellationToken);

        // Assert
        var doses = await ExecuteDbContextAsync(db => db.Set<VaccinationRecord>()
            .Where(x => x.PersonId == input.PersonId)
            .Select(x => x.DoseNumber)
            .ToListAsync(cancellationToken));
        doses.ShouldBe([(short)1, (short)2], ignoreOrder: true);
    }

    [Fact]
    public async Task Should_Throw_Not_Found_When_Person_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = await MakeRegisterVaccinationInputAsync(cancellationToken) with { PersonId = Guid.CreateVersion7() };

        // Act
        var act = () => SendAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("person.not_found");
    }

    [Fact]
    public async Task Should_Throw_Not_Found_When_Vaccine_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = await MakeRegisterVaccinationInputAsync(cancellationToken) with { VaccineId = Guid.CreateVersion7() };

        // Act
        var act = () => SendAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("vaccine.not_found");
    }

    [Fact]
    [Trait("RN", "RN11")]
    public async Task Should_Reject_Dose_When_It_Was_Registered_Before()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = await MakeRegisterVaccinationInputAsync(cancellationToken);
        await SendAsync(input, cancellationToken);

        // Act
        var act = () => SendAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<ConflictException>(act);
        exception.Code.ShouldBe("vaccination.dose_already_registered");
    }

    [Fact]
    [Trait("RN", "RN12")]
    public async Task Should_Store_Vaccination_When_Date_Is_Today_In_Brasilia()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = await MakeRegisterVaccinationInputAsync(cancellationToken) with { AppliedOn = ApiFactory.Today };

        // Act
        var output = await SendAsync(input, cancellationToken);

        // Assert
        var record = await ExecuteDbContextAsync(db => db.Set<VaccinationRecord>().SingleAsync(x => x.Id == output.Id, cancellationToken));
        record.AppliedOn.ShouldBe(ApiFactory.Today);
    }

    [Fact]
    [Trait("RN", "RN12")]
    public async Task Should_Reject_Date_When_It_Is_After_Today_In_Brasilia()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = await MakeRegisterVaccinationInputAsync(cancellationToken) with { AppliedOn = ApiFactory.Today.AddDays(1) };

        // Act
        var act = () => SendAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.applied_on_in_future");
    }

    // A person and a vaccine in the database, and the first dose of the vaccine.
    private async Task<RegisterVaccinationCommand> MakeRegisterVaccinationInputAsync(CancellationToken cancellationToken)
    {
        var person = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        return new RegisterVaccinationCommand(person.Id, vaccine.Id, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15));
    }
}

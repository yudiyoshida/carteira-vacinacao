using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.FindVaccinationById;
using VaccinationCard.Api.Modules.People.Application.UseCases.RegisterVaccination;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.IntegrationTests.Modules.People.Application.UseCases.FindVaccinationById;

public sealed class FindVaccinationByIdHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Should_Return_Vaccination_When_It_Belongs_To_The_Person()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var person = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        var dose = new DoseDto(DoseType.Primary, 1);
        var appliedOn = new DateOnly(2020, 1, 15);
        var registered = await SendAsync(new RegisterVaccinationCommand(person.Id, vaccine.Id, dose, appliedOn), cancellationToken);

        // Act
        var output = await SendAsync(new FindVaccinationByIdQuery(person.Id, registered.Id), cancellationToken);

        // Assert
        output.ShouldBe(new VaccinationDto(registered.Id, vaccine.Id, dose, appliedOn));
    }

    [Fact]
    public async Task Should_Throw_Person_Not_Found_When_Person_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new FindVaccinationByIdQuery(Guid.CreateVersion7(), Guid.CreateVersion7());

        // Act
        var act = () => SendAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("person.not_found");
    }

    // The vaccination exists, but belongs to another person.
    [Fact]
    public async Task Should_Throw_Vaccination_Not_Found_When_Vaccination_Belongs_To_Another_Person()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var owner = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var other = await SendAsync(new CreatePersonCommand("Ana Conceição", "93541134780"), cancellationToken);
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        var registered = await SendAsync(new RegisterVaccinationCommand(owner.Id, vaccine.Id, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15)), cancellationToken);
        var input = new FindVaccinationByIdQuery(other.Id, registered.Id);

        // Act
        var act = () => SendAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("vaccination.not_found");
    }
}

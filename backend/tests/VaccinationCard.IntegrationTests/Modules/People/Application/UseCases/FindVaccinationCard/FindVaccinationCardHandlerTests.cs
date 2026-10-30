using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.FindVaccinationCard;
using VaccinationCard.Api.Modules.People.Application.UseCases.RegisterVaccination;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.IntegrationTests.Modules.People.Application.UseCases.FindVaccinationCard;

public sealed class FindVaccinationCardHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    // The vaccines are registered out of name order. BCG ends complete, DTP has the primary series and the first
    // booster, and Hepatite B has two of its three doses.
    [Fact]
    [Trait("RN", "RN17")]
    [Trait("RN", "RN26")]
    public async Task Should_Return_Card_With_Vaccines_In_Name_Order_And_Doses_In_Sequence_When_Person_Has_Vaccinations()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var person = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var hepatitisB = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        var dtp = await SendAsync(new CreateVaccineCommand("DTP", 3, 2), cancellationToken);
        var bcg = await SendAsync(new CreateVaccineCommand("BCG", 1, 0), cancellationToken);
        await RegisterAsync(person.Id, hepatitisB.Id, DoseType.Primary, 1, new DateOnly(2020, 1, 15), cancellationToken);
        await RegisterAsync(person.Id, hepatitisB.Id, DoseType.Primary, 2, new DateOnly(2020, 2, 15), cancellationToken);
        var dtpFirst = await RegisterAsync(person.Id, dtp.Id, DoseType.Primary, 1, new DateOnly(2020, 3, 15), cancellationToken);
        var dtpSecond = await RegisterAsync(person.Id, dtp.Id, DoseType.Primary, 2, new DateOnly(2020, 5, 15), cancellationToken);
        var dtpThird = await RegisterAsync(person.Id, dtp.Id, DoseType.Primary, 3, new DateOnly(2020, 7, 15), cancellationToken);
        var dtpBooster = await RegisterAsync(person.Id, dtp.Id, DoseType.Booster, 1, new DateOnly(2021, 7, 15), cancellationToken);
        await RegisterAsync(person.Id, bcg.Id, DoseType.Primary, 1, new DateOnly(2020, 1, 15), cancellationToken);

        // Act
        var card = await SendAsync(new FindVaccinationCardQuery(person.Id), cancellationToken);

        // Assert
        card.Person.ShouldBe(new PersonDto(person.Id, "Maria Silva", "123.456.789-09"));
        card.Vaccines.Select(entry => entry.VaccineId).ShouldBe([bcg.Id, dtp.Id, hepatitisB.Id]);
        var (bcgEntry, dtpEntry, hepatitisBEntry) = (card.Vaccines[0], card.Vaccines[1], card.Vaccines[2]);
        dtpEntry.VaccineName.ShouldBe("DTP");
        dtpEntry.Scheme.ShouldBe(new DoseSchemeDto(3, 2));
        dtpEntry.Doses.ShouldBe(
        [
            new VaccinationCardDoseDto(dtpFirst, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 3, 15)),
            new VaccinationCardDoseDto(dtpSecond, new DoseDto(DoseType.Primary, 2), new DateOnly(2020, 5, 15)),
            new VaccinationCardDoseDto(dtpThird, new DoseDto(DoseType.Primary, 3), new DateOnly(2020, 7, 15)),
            new VaccinationCardDoseDto(dtpBooster, new DoseDto(DoseType.Booster, 1), new DateOnly(2021, 7, 15)),
        ]);
        dtpEntry.Progress.ShouldBe(new VaccinationCardProgressDto(3, 3, true, 1, 2));
        dtpEntry.NextExpectedDose.ShouldBe(new DoseDto(DoseType.Booster, 2));
        bcgEntry.Progress.ShouldBe(new VaccinationCardProgressDto(1, 1, true, 0, 0));
        bcgEntry.NextExpectedDose.ShouldBeNull();
        hepatitisBEntry.Progress.ShouldBe(new VaccinationCardProgressDto(2, 3, false, 0, 0));
        hepatitisBEntry.NextExpectedDose.ShouldBe(new DoseDto(DoseType.Primary, 3));
    }

    [Fact]
    public async Task Should_Return_Card_Without_Vaccines_When_Person_Has_No_Vaccination()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var person = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);

        // Act
        var card = await SendAsync(new FindVaccinationCardQuery(person.Id), cancellationToken);

        // Assert
        card.Person.Id.ShouldBe(person.Id);
        card.Vaccines.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Throw_Not_Found_When_Person_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new FindVaccinationCardQuery(Guid.CreateVersion7());

        // Act
        var act = () => SendAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("person.not_found");
    }

    private async Task<Guid> RegisterAsync(Guid personId, Guid vaccineId, DoseType type, int number, DateOnly appliedOn, CancellationToken cancellationToken)
    {
        var output = await SendAsync(new RegisterVaccinationCommand(personId, vaccineId, new DoseDto(type, number), appliedOn), cancellationToken);
        return output.Id;
    }
}

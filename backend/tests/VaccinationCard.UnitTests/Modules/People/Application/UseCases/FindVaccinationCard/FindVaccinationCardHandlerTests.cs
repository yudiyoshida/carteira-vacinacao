using MediatR;
using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.People.Application.UseCases.FindVaccinationCard;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindVaccinesByIds;
using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.UnitTests.Modules.People.Application.UseCases.FindVaccinationCard;

public sealed class FindVaccinationCardHandlerTests
{
    private static readonly PersonDto Person = new(Guid.CreateVersion7(), "Maria Silva", "12345678909");
    private static readonly VaccineDto Bcg = new(Guid.CreateVersion7(), "BCG", 1, 0);
    private static readonly VaccineDto Dtp = new(Guid.CreateVersion7(), "DTP", 3, 2);
    private static readonly VaccineDto HepatitisB = new(Guid.CreateVersion7(), "Hepatite B", 3, 0);

    private readonly IPersonDao _dao = Substitute.For<IPersonDao>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly FindVaccinationCardHandler _sut;

    public FindVaccinationCardHandlerTests() => _sut = new FindVaccinationCardHandler(_dao, _sender);

    [Fact]
    [Trait("RN", "RN26")]
    public async Task Should_Return_Person_With_Formatted_Cpf_When_Person_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        ArrangeCard([]);

        // Act
        var output = await _sut.Handle(MakeFindVaccinationCardInput(), cancellationToken);

        // Assert
        output.Person.ShouldBe(Person with { Cpf = "123.456.789-09" });
    }

    // Vaccines answers in name order, and the DAO returns the vaccinations in another order.
    [Fact]
    [Trait("RN", "RN17")]
    public async Task Should_List_Vaccines_In_The_Name_Order_Of_The_Query_When_Building_The_Card()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        ArrangeCard(
            [
                MakeVaccination(HepatitisB, DoseType.Primary, 1, new DateOnly(2020, 1, 15)),
                MakeVaccination(Bcg, DoseType.Primary, 1, new DateOnly(2020, 1, 15)),
                MakeVaccination(Dtp, DoseType.Primary, 1, new DateOnly(2020, 1, 15)),
            ],
            Bcg,
            Dtp,
            HepatitisB
        );

        // Act
        var output = await _sut.Handle(MakeFindVaccinationCardInput(), cancellationToken);

        // Assert
        output.Vaccines.Select(entry => entry.VaccineName).ShouldBe(["BCG", "DTP", "Hepatite B"]);
    }

    [Fact]
    [Trait("RN", "RN17")]
    public async Task Should_List_Doses_In_Sequence_Order_When_Vaccinations_Come_Out_Of_Order()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var firstBooster = MakeVaccination(Dtp, DoseType.Booster, 1, new DateOnly(2021, 1, 15));
        var secondDose = MakeVaccination(Dtp, DoseType.Primary, 2, new DateOnly(2020, 3, 15));
        var firstDose = MakeVaccination(Dtp, DoseType.Primary, 1, new DateOnly(2020, 1, 15));
        var thirdDose = MakeVaccination(Dtp, DoseType.Primary, 3, new DateOnly(2020, 5, 15));
        ArrangeCard([firstBooster, secondDose, firstDose, thirdDose], Dtp);

        // Act
        var output = await _sut.Handle(MakeFindVaccinationCardInput(), cancellationToken);

        // Assert
        var entry = output.Vaccines.ShouldHaveSingleItem();
        entry.Doses.ShouldBe(
        [
            new VaccinationCardDoseDto(firstDose.Id, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15)),
            new VaccinationCardDoseDto(secondDose.Id, new DoseDto(DoseType.Primary, 2), new DateOnly(2020, 3, 15)),
            new VaccinationCardDoseDto(thirdDose.Id, new DoseDto(DoseType.Primary, 3), new DateOnly(2020, 5, 15)),
            new VaccinationCardDoseDto(firstBooster.Id, new DoseDto(DoseType.Booster, 1), new DateOnly(2021, 1, 15)),
        ]);
    }

    [Fact]
    [Trait("RN", "RN17")]
    public async Task Should_Show_Scheme_And_Progress_When_Primary_Series_Is_Incomplete()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        ArrangeCard(
            [
                MakeVaccination(HepatitisB, DoseType.Primary, 1, new DateOnly(2020, 1, 15)),
                MakeVaccination(HepatitisB, DoseType.Primary, 2, new DateOnly(2020, 2, 15)),
            ],
            HepatitisB
        );

        // Act
        var output = await _sut.Handle(MakeFindVaccinationCardInput(), cancellationToken);

        // Assert
        var entry = output.Vaccines.ShouldHaveSingleItem();
        entry.VaccineId.ShouldBe(HepatitisB.Id);
        entry.VaccineName.ShouldBe("Hepatite B");
        entry.Scheme.ShouldBe(new DoseSchemeDto(3, 0));
        entry.Progress.ShouldBe(new VaccinationCardProgressDto(2, 3, false, 0, 0));
    }

    [Fact]
    [Trait("RN", "RN17")]
    public async Task Should_Show_Primary_Series_Complete_When_Every_Primary_Dose_Is_Applied()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        ArrangeCard(
            [
                MakeVaccination(Dtp, DoseType.Primary, 1, new DateOnly(2020, 1, 15)),
                MakeVaccination(Dtp, DoseType.Primary, 2, new DateOnly(2020, 3, 15)),
                MakeVaccination(Dtp, DoseType.Primary, 3, new DateOnly(2020, 5, 15)),
                MakeVaccination(Dtp, DoseType.Booster, 1, new DateOnly(2021, 1, 15)),
            ],
            Dtp
        );

        // Act
        var output = await _sut.Handle(MakeFindVaccinationCardInput(), cancellationToken);

        // Assert
        var entry = output.Vaccines.ShouldHaveSingleItem();
        entry.Scheme.ShouldBe(new DoseSchemeDto(3, 2));
        entry.Progress.ShouldBe(new VaccinationCardProgressDto(3, 3, true, 1, 2));
    }

    [Fact]
    [Trait("RN", "RN10")]
    public async Task Should_Expect_The_Next_Primary_Dose_When_Primary_Series_Is_In_Progress()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        ArrangeCard(
            [
                MakeVaccination(HepatitisB, DoseType.Primary, 1, new DateOnly(2020, 1, 15)),
                MakeVaccination(HepatitisB, DoseType.Primary, 2, new DateOnly(2020, 2, 15)),
            ],
            HepatitisB
        );

        // Act
        var output = await _sut.Handle(MakeFindVaccinationCardInput(), cancellationToken);

        // Assert
        output.Vaccines.ShouldHaveSingleItem().NextExpectedDose.ShouldBe(new DoseDto(DoseType.Primary, 3));
    }

    [Fact]
    [Trait("RN", "RN10")]
    public async Task Should_Expect_The_First_Booster_When_Primary_Series_Is_Complete()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        ArrangeCard(
            [
                MakeVaccination(Dtp, DoseType.Primary, 1, new DateOnly(2020, 1, 15)),
                MakeVaccination(Dtp, DoseType.Primary, 2, new DateOnly(2020, 3, 15)),
                MakeVaccination(Dtp, DoseType.Primary, 3, new DateOnly(2020, 5, 15)),
            ],
            Dtp
        );

        // Act
        var output = await _sut.Handle(MakeFindVaccinationCardInput(), cancellationToken);

        // Assert
        output.Vaccines.ShouldHaveSingleItem().NextExpectedDose.ShouldBe(new DoseDto(DoseType.Booster, 1));
    }

    [Fact]
    [Trait("RN", "RN10")]
    public async Task Should_Expect_No_Dose_When_Scheme_Is_Complete()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        ArrangeCard([MakeVaccination(Bcg, DoseType.Primary, 1, new DateOnly(2020, 1, 15))], Bcg);

        // Act
        var output = await _sut.Handle(MakeFindVaccinationCardInput(), cancellationToken);

        // Assert
        output.Vaccines.ShouldHaveSingleItem().NextExpectedDose.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Ask_Vaccines_For_Each_Vaccine_Once_When_Building_The_Card()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        ArrangeCard(
            [
                MakeVaccination(HepatitisB, DoseType.Primary, 1, new DateOnly(2020, 1, 15)),
                MakeVaccination(HepatitisB, DoseType.Primary, 2, new DateOnly(2020, 2, 15)),
                MakeVaccination(Bcg, DoseType.Primary, 1, new DateOnly(2020, 1, 15)),
            ],
            Bcg,
            HepatitisB
        );

        // Act
        await _sut.Handle(MakeFindVaccinationCardInput(), cancellationToken);

        // Assert
        await _sender.Received(1).Send(
            Arg.Is<FindVaccinesByIdsQuery>(query => query.Ids.Count == 2 && query.Ids.Contains(HepatitisB.Id) && query.Ids.Contains(Bcg.Id)),
            cancellationToken
        );
    }

    [Fact]
    public async Task Should_Return_Card_Without_Vaccines_When_Person_Has_No_Vaccination()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        ArrangeCard([]);

        // Act
        var output = await _sut.Handle(MakeFindVaccinationCardInput(), cancellationToken);

        // Assert
        output.Vaccines.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Throw_Not_Found_When_Person_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new FindVaccinationCardQuery(Guid.CreateVersion7());
        _dao.FindCardDataAsync(input.PersonId, Arg.Any<CancellationToken>()).Returns((VaccinationCardDataDto?)null);

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("person.not_found");
        await _sender.DidNotReceive().Send(Arg.Any<FindVaccinesByIdsQuery>(), Arg.Any<CancellationToken>());
    }

    // The DAO returns the person with the vaccinations, and Vaccines returns the vaccines, in name order. A query record
    // compares its list of ids by reference, so the mock answers any FindVaccinesByIdsQuery.
    private void ArrangeCard(IReadOnlyList<VaccinationDto> vaccinations, params VaccineDto[] vaccines)
    {
        _dao.FindCardDataAsync(Person.Id, Arg.Any<CancellationToken>()).Returns(new VaccinationCardDataDto(Person, vaccinations));
        _sender.Send(Arg.Any<FindVaccinesByIdsQuery>(), Arg.Any<CancellationToken>()).Returns(vaccines);
    }

    private static VaccinationDto MakeVaccination(VaccineDto vaccine, DoseType type, int number, DateOnly appliedOn) =>
        new(Guid.CreateVersion7(), vaccine.Id, new DoseDto(type, number), appliedOn);

    private static FindVaccinationCardQuery MakeFindVaccinationCardInput() => new(Person.Id);
}

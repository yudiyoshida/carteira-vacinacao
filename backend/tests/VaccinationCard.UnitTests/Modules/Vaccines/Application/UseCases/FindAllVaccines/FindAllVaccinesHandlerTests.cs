using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindAllVaccines;
using VaccinationCard.Api.Shared.Pagination;

namespace VaccinationCard.UnitTests.Modules.Vaccines.Application.UseCases.FindAllVaccines;

public sealed class FindAllVaccinesHandlerTests
{
    private readonly IVaccineDao _dao = Substitute.For<IVaccineDao>();
    private readonly FindAllVaccinesHandler _sut;

    public FindAllVaccinesHandlerTests() => _sut = new FindAllVaccinesHandler(_dao);

    [Fact]
    public async Task Should_Return_Page_From_Dao_When_Query_Is_Valid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeFindAllVaccinesInput() with { Page = 2, Size = 10, Search = "hepa" };
        var page = Paginated<VaccineDto>.Create([new VaccineDto(Guid.CreateVersion7(), "Hepatite B", 3, 0)], totalItems: 11, currentPage: 2, itemsPerPage: 10);
        _dao.FindAllAsync("hepa", 2, 10, Arg.Any<CancellationToken>()).Returns(page);

        // Act
        var output = await _sut.Handle(input, cancellationToken);

        // Assert
        output.ShouldBe(page);
    }

    [Fact]
    [Trait("RN", "RN20")]
    public async Task Should_Read_Every_Vaccine_From_First_Page_When_Page_And_Size_Are_Missing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeFindAllVaccinesInput();

        // Act
        await _sut.Handle(input, cancellationToken);

        // Assert
        await _dao.Received(1).FindAllAsync(null, 1, null, cancellationToken);
    }

    [Fact]
    [Trait("RN", "RN20")]
    public async Task Should_Trim_Search_When_Search_Has_Surrounding_Spaces()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeFindAllVaccinesInput() with { Search = "  hepa  " };

        // Act
        await _sut.Handle(input, cancellationToken);

        // Assert
        await _dao.Received(1).FindAllAsync("hepa", 1, null, cancellationToken);
    }

    private static FindAllVaccinesQuery MakeFindAllVaccinesInput() => new(null, null, null);
}

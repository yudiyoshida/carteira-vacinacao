using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.People.Application.UseCases.FindAllPeople;
using VaccinationCard.Api.Shared.Pagination;

namespace VaccinationCard.UnitTests.Modules.People.Application.UseCases.FindAllPeople;

public sealed class FindAllPeopleHandlerTests
{
    private readonly IPersonDao _dao = Substitute.For<IPersonDao>();
    private readonly FindAllPeopleHandler _sut;

    // The handler reads the page it gets to mask the CPF, so the DAO answers an empty page unless a test sets another.
    public FindAllPeopleHandlerTests()
    {
        _dao.FindAllAsync(default, default, default, default)
            .ReturnsForAnyArgs(Paginated<PersonSummaryDto>.Create([], totalItems: 0, currentPage: 1, itemsPerPage: 20));
        _sut = new FindAllPeopleHandler(_dao);
    }

    [Fact]
    [Trait("RN", "RN26")]
    public async Task Should_Return_Page_With_Masked_Cpf_When_Query_Is_Valid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeFindAllPeopleInput() with { Page = 2, Size = 10, Search = "maria" };
        var person = new PersonSummaryDto(Guid.CreateVersion7(), "Maria Silva", "12345678909");
        _dao.FindAllAsync("maria", 2, 10, Arg.Any<CancellationToken>())
            .Returns(Paginated<PersonSummaryDto>.Create([person], totalItems: 11, currentPage: 2, itemsPerPage: 10));

        // Act
        var output = await _sut.Handle(input, cancellationToken);

        // Assert
        output.CurrentPage.ShouldBe(2);
        output.ItemsPerPage.ShouldBe(10);
        output.TotalItems.ShouldBe(11);
        output.TotalPages.ShouldBe(2);
        output.Data.ShouldBe([person with { Cpf = "***.456.789-**" }]);
    }

    [Fact]
    [Trait("RN", "RN19")]
    public async Task Should_Read_First_Page_Of_20_People_When_Page_And_Size_Are_Missing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeFindAllPeopleInput();

        // Act
        await _sut.Handle(input, cancellationToken);

        // Assert
        await _dao.Received(1).FindAllAsync(null, 1, 20, cancellationToken);
    }

    [Fact]
    [Trait("RN", "RN19")]
    public async Task Should_Trim_Search_When_Search_Has_Surrounding_Spaces()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeFindAllPeopleInput() with { Search = "  maria  " };

        // Act
        await _sut.Handle(input, cancellationToken);

        // Assert
        await _dao.Received(1).FindAllAsync("maria", 1, 20, cancellationToken);
    }

    private static FindAllPeopleQuery MakeFindAllPeopleInput() => new(null, null, null);
}

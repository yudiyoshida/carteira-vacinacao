using VaccinationCard.Api.Shared.Pagination;

namespace VaccinationCard.UnitTests.Shared.Pagination;

public sealed class PaginatedTests
{
    [Fact]
    public void Should_Round_Total_Pages_Up_When_Last_Page_Is_Partial()
    {
        // Arrange
        string[] data = ["BCG", "DTP"];

        // Act
        var page = Paginated<string>.Create(data, totalItems: 5, currentPage: 1, itemsPerPage: 2);

        // Assert
        page.CurrentPage.ShouldBe(1);
        page.ItemsPerPage.ShouldBe(2);
        page.TotalItems.ShouldBe(5);
        page.TotalPages.ShouldBe(3);
        page.Data.ShouldBe(data);
    }

    [Fact]
    public void Should_Have_One_Page_When_Page_Holds_Every_Item()
    {
        // Arrange
        string[] data = ["BCG", "DTP", "Hepatite B"];

        // Act
        var page = Paginated<string>.Create(data, totalItems: 3, currentPage: 1, itemsPerPage: 3);

        // Assert
        page.TotalPages.ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(20)]
    public void Should_Have_No_Pages_When_List_Is_Empty(int itemsPerPage)
    {
        // Arrange
        string[] data = [];

        // Act
        var page = Paginated<string>.Create(data, totalItems: 0, currentPage: 1, itemsPerPage);

        // Assert
        page.ItemsPerPage.ShouldBe(itemsPerPage);
        page.TotalPages.ShouldBe(0);
        page.Data.ShouldBeEmpty();
    }
}

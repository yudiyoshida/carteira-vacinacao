using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindAllVaccines;

namespace VaccinationCard.IntegrationTests.Modules.Vaccines.Application.UseCases.FindAllVaccines;

public sealed class FindAllVaccinesHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    [Trait("RN", "RN20")]
    public async Task Should_Return_Every_Vaccine_In_One_Page_When_Page_And_Size_Are_Missing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateVaccinesAsync(["Hepatite B", "BCG", "DTP"], cancellationToken);
        var input = MakeFindAllVaccinesInput();

        // Act
        var page = await SendAsync(input, cancellationToken);

        // Assert
        page.CurrentPage.ShouldBe(1);
        page.ItemsPerPage.ShouldBe(3);
        page.TotalItems.ShouldBe(3);
        page.TotalPages.ShouldBe(1);
        page.Data.Select(vaccine => vaccine.Name).ShouldBe(["BCG", "DTP", "Hepatite B"]);
    }

    [Fact]
    [Trait("RN", "RN20")]
    public async Task Should_Return_Requested_Page_Of_The_Search_When_Search_Has_Surrounding_Spaces()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateVaccinesAsync(["Hepatite C", "BCG", "Hepatite A", "Hepatite B"], cancellationToken);
        var input = MakeFindAllVaccinesInput() with { Page = 2, Size = 2, Search = "  hepatite  " };

        // Act
        var page = await SendAsync(input, cancellationToken);

        // Assert
        page.CurrentPage.ShouldBe(2);
        page.TotalItems.ShouldBe(3);
        page.TotalPages.ShouldBe(2);
        page.Data.Select(vaccine => vaccine.Name).ShouldBe(["Hepatite C"]);
    }

    private async Task CreateVaccinesAsync(string[] names, CancellationToken cancellationToken)
    {
        foreach (var name in names)
        {
            await SendAsync(new CreateVaccineCommand(name, 1, 0), cancellationToken);
        }
    }

    private static FindAllVaccinesQuery MakeFindAllVaccinesInput() => new(null, null, null);
}

using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.FindAllPeople;

namespace VaccinationCard.IntegrationTests.Modules.People.Application.UseCases.FindAllPeople;

public sealed class FindAllPeopleHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    [Trait("RN", "RN19")]
    [Trait("RN", "RN26")]
    public async Task Should_Return_First_Page_Of_20_With_Masked_Cpf_When_Page_And_Size_Are_Missing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var maria = await SendAsync(new CreatePersonCommand("Maria Souza", "52998224725"), cancellationToken);
        var ana = await SendAsync(new CreatePersonCommand("Ana Conceição", "93541134780"), cancellationToken);
        var input = MakeFindAllPeopleInput();

        // Act
        var page = await SendAsync(input, cancellationToken);

        // Assert
        page.CurrentPage.ShouldBe(1);
        page.ItemsPerPage.ShouldBe(20);
        page.TotalItems.ShouldBe(2);
        page.TotalPages.ShouldBe(1);
        page.Data.ShouldBe(
        [
            new PersonSummaryDto(ana.Id, "Ana Conceição", "***.411.347-**"),
            new PersonSummaryDto(maria.Id, "Maria Souza", "***.982.247-**"),
        ]);
    }

    [Fact]
    [Trait("RN", "RN19")]
    public async Task Should_Return_Requested_Page_Of_The_Search_When_Search_Has_Surrounding_Spaces()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        await SendAsync(new CreatePersonCommand("Ana Conceição", "93541134780"), cancellationToken);
        await SendAsync(new CreatePersonCommand("João da Silva", "11144477735"), cancellationToken);
        var input = MakeFindAllPeopleInput() with { Page = 2, Size = 1, Search = "  silva  " };

        // Act
        var page = await SendAsync(input, cancellationToken);

        // Assert
        page.CurrentPage.ShouldBe(2);
        page.ItemsPerPage.ShouldBe(1);
        page.TotalItems.ShouldBe(2);
        page.TotalPages.ShouldBe(2);
        page.Data.Select(person => person.Name).ShouldBe(["Maria Silva"]);
    }

    // The page size stays the one applied, even when no person matches (TechSpec §7.1).
    [Fact]
    [Trait("RN", "RN19")]
    public async Task Should_Return_Empty_Envelope_With_Default_Size_When_No_Person_Matches()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var input = MakeFindAllPeopleInput() with { Search = "joão" };

        // Act
        var page = await SendAsync(input, cancellationToken);

        // Assert
        page.CurrentPage.ShouldBe(1);
        page.ItemsPerPage.ShouldBe(20);
        page.TotalItems.ShouldBe(0);
        page.TotalPages.ShouldBe(0);
        page.Data.ShouldBeEmpty();
    }

    private static FindAllPeopleQuery MakeFindAllPeopleInput() => new(null, null, null);
}

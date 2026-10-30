using VaccinationCard.Api.Modules.People.Application.UseCases.FindAllPeople;

namespace VaccinationCard.UnitTests.Modules.People.Application.UseCases.FindAllPeople;

public sealed class FindAllPeopleValidatorTests
{
    private readonly FindAllPeopleValidator _sut = new();

    public static TheoryData<string> SearchesWithinLimit => new() { new string('a', 100), $"  {new string('a', 100)}  " };

    [Fact]
    public void Should_Accept_Input_When_Parameters_Are_Missing()
    {
        // Arrange
        var input = MakeFindAllPeopleInput();

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [Trait("RN", "RN19")]
    [InlineData(1, 1)]
    [InlineData(1, 100)]
    [InlineData(int.MaxValue, 100)]
    public void Should_Accept_Input_When_Page_And_Size_Are_Within_The_Limits(int page, int size)
    {
        // Arrange
        var input = MakeFindAllPeopleInput() with { Page = page, Size = size };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(SearchesWithinLimit))]
    public void Should_Accept_Search_When_Length_Without_Surrounding_Spaces_Is_Within_The_Limit(string search)
    {
        // Arrange
        var input = MakeFindAllPeopleInput() with { Search = search };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Reject_Input_When_Page_Is_Less_Than_One(int page)
    {
        // Arrange
        var input = MakeFindAllPeopleInput() with { Page = page };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(FindAllPeopleQuery.Page));
    }

    [Theory]
    [Trait("RN", "RN19")]
    [InlineData(0)]
    [InlineData(101)]
    public void Should_Reject_Input_When_Size_Is_Out_Of_Range(int size)
    {
        // Arrange
        var input = MakeFindAllPeopleInput() with { Size = size };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(FindAllPeopleQuery.Size));
    }

    [Fact]
    public void Should_Reject_Input_When_Search_Is_Too_Long()
    {
        // Arrange
        var input = MakeFindAllPeopleInput() with { Search = new string('a', 101) };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(FindAllPeopleQuery.Search));
    }

    private static FindAllPeopleQuery MakeFindAllPeopleInput() => new(null, null, null);
}

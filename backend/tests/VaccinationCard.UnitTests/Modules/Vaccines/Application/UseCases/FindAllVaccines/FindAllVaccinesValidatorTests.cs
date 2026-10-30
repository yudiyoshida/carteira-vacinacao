using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindAllVaccines;

namespace VaccinationCard.UnitTests.Modules.Vaccines.Application.UseCases.FindAllVaccines;

public sealed class FindAllVaccinesValidatorTests
{
    private readonly FindAllVaccinesValidator _sut = new();

    public static TheoryData<string> SearchesWithinLimit => new() { new string('a', 100), $"  {new string('a', 100)}  " };

    [Fact]
    public void Should_Accept_Input_When_Parameters_Are_Missing()
    {
        // Arrange
        var input = MakeFindAllVaccinesInput();

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 100)]
    [InlineData(int.MaxValue, 100)]
    public void Should_Accept_Input_When_Page_And_Size_Are_Within_The_Limits(int page, int size)
    {
        // Arrange
        var input = MakeFindAllVaccinesInput() with { Page = page, Size = size };

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
        var input = MakeFindAllVaccinesInput() with { Search = search };

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
        var input = MakeFindAllVaccinesInput() with { Page = page };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(FindAllVaccinesQuery.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Should_Reject_Input_When_Size_Is_Out_Of_Range(int size)
    {
        // Arrange
        var input = MakeFindAllVaccinesInput() with { Size = size };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(FindAllVaccinesQuery.Size));
    }

    [Fact]
    public void Should_Reject_Input_When_Search_Is_Too_Long()
    {
        // Arrange
        var input = MakeFindAllVaccinesInput() with { Search = new string('a', 101) };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(FindAllVaccinesQuery.Search));
    }

    private static FindAllVaccinesQuery MakeFindAllVaccinesInput() => new(null, null, null);
}

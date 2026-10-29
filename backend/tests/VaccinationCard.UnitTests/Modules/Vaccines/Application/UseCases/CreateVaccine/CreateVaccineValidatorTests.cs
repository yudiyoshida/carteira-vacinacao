using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;

namespace VaccinationCard.UnitTests.Modules.Vaccines.Application.UseCases.CreateVaccine;

public sealed class CreateVaccineValidatorTests
{
    private readonly CreateVaccineValidator _sut = new();

    public static TheoryData<string> NamesWithinRange => new() { "BC", "  BC  ", new string('A', 100), $"  {new string('A', 100)}  " };

    [Fact]
    public void Should_Accept_Input_When_Every_Field_Is_Valid()
    {
        // Arrange
        var input = MakeCreateVaccineInput();

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [Trait("RN", "RN05")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    [InlineData("  A  ")]
    public void Should_Reject_Input_When_Name_Is_Missing_Or_Too_Short(string? name)
    {
        // Arrange
        var input = MakeCreateVaccineInput() with { Name = name! };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateVaccineCommand.Name));
    }

    [Fact]
    [Trait("RN", "RN05")]
    public void Should_Reject_Input_When_Name_Is_Too_Long()
    {
        // Arrange
        var input = MakeCreateVaccineInput() with { Name = new string('A', 101) };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateVaccineCommand.Name));
    }

    [Theory]
    [Trait("RN", "RN05")]
    [MemberData(nameof(NamesWithinRange))]
    public void Should_Accept_Name_When_Length_Without_Surrounding_Spaces_Is_Within_Range(string name)
    {
        // Arrange
        var input = MakeCreateVaccineInput() with { Name = name };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [Trait("RN", "RN06")]
    [InlineData(0)]
    [InlineData(11)]
    public void Should_Reject_Input_When_Primary_Doses_Are_Out_Of_Range(int primaryDoses)
    {
        // Arrange
        var input = MakeCreateVaccineInput() with { PrimaryDoses = primaryDoses };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateVaccineCommand.PrimaryDoses));
    }

    [Theory]
    [Trait("RN", "RN06")]
    [InlineData(-1)]
    [InlineData(6)]
    public void Should_Reject_Input_When_Booster_Doses_Are_Out_Of_Range(int boosterDoses)
    {
        // Arrange
        var input = MakeCreateVaccineInput() with { BoosterDoses = boosterDoses };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateVaccineCommand.BoosterDoses));
    }

    [Theory]
    [Trait("RN", "RN06")]
    [InlineData(1, 0)]
    [InlineData(10, 5)]
    public void Should_Accept_Scheme_When_Doses_Are_At_The_Limits(int primaryDoses, int boosterDoses)
    {
        // Arrange
        var input = MakeCreateVaccineInput() with { PrimaryDoses = primaryDoses, BoosterDoses = boosterDoses };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    private static CreateVaccineCommand MakeCreateVaccineInput() => new("Hepatite B", 3, 0);
}

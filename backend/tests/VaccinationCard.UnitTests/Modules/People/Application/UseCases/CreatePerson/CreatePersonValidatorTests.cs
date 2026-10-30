using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;

namespace VaccinationCard.UnitTests.Modules.People.Application.UseCases.CreatePerson;

public sealed class CreatePersonValidatorTests
{
    private readonly CreatePersonValidator _sut = new();

    public static TheoryData<string> NamesWithinRange => new() { "Ana", "  Ana  ", new string('A', 150), $"  {new string('A', 150)}  " };

    [Fact]
    public void Should_Accept_Input_When_Every_Field_Is_Valid()
    {
        // Arrange
        var input = MakeCreatePersonInput();

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [Trait("RN", "RN01")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("AB")]
    [InlineData("  AB  ")]
    public void Should_Reject_Input_When_Name_Is_Missing_Or_Too_Short(string? name)
    {
        // Arrange
        var input = MakeCreatePersonInput() with { Name = name! };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreatePersonCommand.Name));
    }

    [Fact]
    [Trait("RN", "RN01")]
    public void Should_Reject_Input_When_Name_Is_Too_Long()
    {
        // Arrange
        var input = MakeCreatePersonInput() with { Name = new string('A', 151) };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreatePersonCommand.Name));
    }

    [Theory]
    [Trait("RN", "RN01")]
    [MemberData(nameof(NamesWithinRange))]
    public void Should_Accept_Name_When_Length_Without_Surrounding_Spaces_Is_Within_Range(string name)
    {
        // Arrange
        var input = MakeCreatePersonInput() with { Name = name };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [Trait("RN", "RN02")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123.456.789-00")]
    [InlineData("111.111.111-11")]
    [InlineData("123.456.789-0")]
    [InlineData("123/456/789-09")]
    public void Should_Reject_Input_When_Cpf_Is_Invalid(string? cpf)
    {
        // Arrange
        var input = MakeCreatePersonInput() with { Cpf = cpf! };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreatePersonCommand.Cpf));
    }

    [Theory]
    [Trait("RN", "RN02")]
    [InlineData("12345678909")]
    [InlineData("123.456.789-09")]
    [InlineData("123.456.78909")]
    [InlineData(" 123 456 789 09 ")]
    public void Should_Accept_Cpf_When_It_Comes_With_Or_Without_Mask(string cpf)
    {
        // Arrange
        var input = MakeCreatePersonInput() with { Cpf = cpf };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    private static CreatePersonCommand MakeCreatePersonInput() => new("Maria Silva", "12345678909");
}

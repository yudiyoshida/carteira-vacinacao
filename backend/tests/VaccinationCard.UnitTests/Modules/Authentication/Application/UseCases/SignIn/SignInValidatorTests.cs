using VaccinationCard.Api.Modules.Authentication.Application.UseCases.SignIn;

namespace VaccinationCard.UnitTests.Modules.Authentication.Application.UseCases.SignIn;

public sealed class SignInValidatorTests
{
    private readonly SignInValidator _sut = new();

    public static TheoryData<string> EmailsWithinRange => new()
    {
        "a@b",
        "  operador@demo.local  ",
        $"{new string('a', 252)}@b",
        $"  {new string('a', 252)}@b  ",
    };

    [Fact]
    public void Should_Accept_Input_When_Every_Field_Is_Valid()
    {
        // Arrange
        var input = MakeSignInInput();

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(EmailsWithinRange))]
    public void Should_Accept_Input_When_Email_Is_Within_Range(string email)
    {
        // Arrange
        var input = MakeSignInInput() with { Email = email };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [Trait("RN", "RN22")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("operador")]
    public void Should_Reject_Input_When_Email_Is_Missing_Or_Malformed(string? email)
    {
        // Arrange
        var input = MakeSignInInput() with { Email = email! };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(SignInCommand.Email));
    }

    [Fact]
    [Trait("RN", "RN22")]
    public void Should_Reject_Input_When_Email_Is_Too_Long()
    {
        // Arrange
        var input = MakeSignInInput() with { Email = $"{new string('a', 253)}@b" };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(SignInCommand.Email));
    }

    [Theory]
    [Trait("RN", "RN22")]
    [InlineData(null)]
    [InlineData("")]
    public void Should_Reject_Input_When_Password_Is_Missing(string? password)
    {
        // Arrange
        var input = MakeSignInInput() with { Password = password! };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(SignInCommand.Password));
    }

    private static SignInCommand MakeSignInInput() => new("operador@demo.local", "Demo@2026!");
}

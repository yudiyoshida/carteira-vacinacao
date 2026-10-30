using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.UnitTests.Shared.ValueObjects;

public sealed class CpfTests
{
    [Theory]
    [Trait("RN", "RN02")]
    [InlineData("12345678909")]
    [InlineData("123.456.789-09")]
    [InlineData("123.456.78909")]
    [InlineData(" 123 456 789 09 ")]
    public void Should_Keep_Only_The_Digits_When_Cpf_Comes_With_Or_Without_Mask(string value)
    {
        // Arrange
        const string digits = "12345678909";

        // Act
        var cpf = Cpf.Create(value);

        // Assert
        cpf.Value.ShouldBe(digits);
    }

    [Fact]
    [Trait("RN", "RN02")]
    public void Should_Accept_Cpf_When_Check_Digits_Are_Valid()
    {
        // Arrange
        const string value = "111.444.777-35";

        // Act
        var isValid = Cpf.IsValid(value);

        // Assert
        isValid.ShouldBeTrue();
    }

    // The check digits of 123.456.789 are 09.
    [Theory]
    [Trait("RN", "RN02")]
    [InlineData("19")]
    [InlineData("08")]
    [InlineData("90")]
    public void Should_Reject_Cpf_When_A_Check_Digit_Is_Wrong(string checkDigits)
    {
        // Arrange
        var value = $"123.456.789-{checkDigits}";

        // Act
        var isValid = Cpf.IsValid(value);

        // Assert
        isValid.ShouldBeFalse();
    }

    // Every one of these sequences passes the check digits.
    [Theory]
    [Trait("RN", "RN02")]
    [InlineData('0')]
    [InlineData('1')]
    [InlineData('2')]
    [InlineData('3')]
    [InlineData('4')]
    [InlineData('5')]
    [InlineData('6')]
    [InlineData('7')]
    [InlineData('8')]
    [InlineData('9')]
    public void Should_Reject_Cpf_When_Every_Digit_Is_The_Same(char digit)
    {
        // Arrange
        var value = new string(digit, 11);

        // Act
        var isValid = Cpf.IsValid(value);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    [Trait("RN", "RN02")]
    public void Should_Reject_Cpf_When_A_Digit_Is_Missing()
    {
        // Arrange
        const string value = "123.456.789-0";

        // Act
        var isValid = Cpf.IsValid(value);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    [Trait("RN", "RN02")]
    public void Should_Reject_Cpf_When_It_Has_A_Digit_Too_Many()
    {
        // Arrange
        const string value = "123.456.789-090";

        // Act
        var isValid = Cpf.IsValid(value);

        // Assert
        isValid.ShouldBeFalse();
    }

    // Only dots, hyphens and spaces are dropped.
    [Theory]
    [Trait("RN", "RN02")]
    [InlineData("/")]
    [InlineData("_")]
    [InlineData("a")]
    public void Should_Reject_Cpf_When_It_Has_A_Character_Other_Than_Digits_Dots_Hyphens_And_Spaces(string character)
    {
        // Arrange
        var value = $"123.456.789{character}09";

        // Act
        var isValid = Cpf.IsValid(value);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    [Trait("RN", "RN02")]
    public void Should_Reject_Cpf_When_It_Has_Only_Dots_Hyphens_And_Spaces()
    {
        // Arrange
        const string value = " ...- ";

        // Act
        var isValid = Cpf.IsValid(value);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    [Trait("RN", "RN02")]
    public void Should_Reject_Cpf_When_It_Is_Missing()
    {
        // Arrange
        const string? value = null;

        // Act
        var isValid = Cpf.IsValid(value);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    [Trait("RN", "RN02")]
    public void Should_Throw_When_Creating_A_Cpf_With_Wrong_Check_Digits()
    {
        // Arrange
        const string value = "123.456.789-00";

        // Act
        var act = () => Cpf.Create(value);

        // Assert
        var exception = Should.Throw<ArgumentException>(act);
        exception.ParamName.ShouldBe(nameof(value));
    }

    [Fact]
    [Trait("RN", "RN26")]
    public void Should_Format_Cpf_When_Showing_It_Whole()
    {
        // Arrange
        var cpf = Cpf.Create("12345678909");

        // Act
        var formatted = cpf.Formatted;

        // Assert
        formatted.ShouldBe("123.456.789-09");
    }

    [Fact]
    [Trait("RN", "RN26")]
    public void Should_Mask_Cpf_When_Showing_It_In_A_List()
    {
        // Arrange
        var cpf = Cpf.Create("12345678909");

        // Act
        var masked = cpf.Masked;

        // Assert
        masked.ShouldBe("***.456.789-**");
    }

    [Fact]
    [Trait("RN", "RN25")]
    public void Should_Return_The_Masked_Cpf_When_Converted_To_Text()
    {
        // Arrange
        var cpf = Cpf.Create("12345678909");

        // Act
        var text = cpf.ToString();

        // Assert
        text.ShouldBe("***.456.789-**");
    }
}

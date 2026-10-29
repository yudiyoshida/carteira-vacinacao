using VaccinationCard.Api.Modules.Vaccines.Domain.ValueObjects;

namespace VaccinationCard.UnitTests.Modules.Vaccines.Domain.ValueObjects;

public sealed class DoseSchemeTests
{
    [Fact]
    [Trait("RN", "RN06")]
    public void Should_Create_Scheme_When_Doses_Are_At_The_Lower_Limits()
    {
        // Arrange
        const int primaryDoses = 1;
        const int boosterDoses = 0;

        // Act
        var scheme = new DoseScheme(primaryDoses, boosterDoses);

        // Assert
        scheme.PrimaryDoses.ShouldBe(primaryDoses);
        scheme.BoosterDoses.ShouldBe(boosterDoses);
    }

    [Fact]
    [Trait("RN", "RN06")]
    public void Should_Create_Scheme_When_Doses_Are_At_The_Upper_Limits()
    {
        // Arrange
        const int primaryDoses = 10;
        const int boosterDoses = 5;

        // Act
        var scheme = new DoseScheme(primaryDoses, boosterDoses);

        // Assert
        scheme.PrimaryDoses.ShouldBe(primaryDoses);
        scheme.BoosterDoses.ShouldBe(boosterDoses);
    }

    [Theory]
    [Trait("RN", "RN06")]
    [InlineData(0)]
    [InlineData(11)]
    public void Should_Reject_Scheme_When_Primary_Doses_Are_Out_Of_Range(int primaryDoses)
    {
        // Arrange
        const int boosterDoses = 0;

        // Act
        var act = () => new DoseScheme(primaryDoses, boosterDoses);

        // Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(act);
        exception.ParamName.ShouldBe(nameof(primaryDoses));
    }

    [Theory]
    [Trait("RN", "RN06")]
    [InlineData(-1)]
    [InlineData(6)]
    public void Should_Reject_Scheme_When_Booster_Doses_Are_Out_Of_Range(int boosterDoses)
    {
        // Arrange
        const int primaryDoses = 1;

        // Act
        var act = () => new DoseScheme(primaryDoses, boosterDoses);

        // Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(act);
        exception.ParamName.ShouldBe(nameof(boosterDoses));
    }
}

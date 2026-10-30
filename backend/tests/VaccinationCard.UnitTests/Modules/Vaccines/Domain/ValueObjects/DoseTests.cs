using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;
using VaccinationCard.Api.Modules.Vaccines.Domain.ValueObjects;

namespace VaccinationCard.UnitTests.Modules.Vaccines.Domain.ValueObjects;

public sealed class DoseTests
{
    [Fact]
    [Trait("RN", "RN08")]
    public void Should_Create_Dose_When_Number_Is_At_The_Lower_Limit()
    {
        // Arrange
        const DoseType type = DoseType.Booster;
        const int number = 1;

        // Act
        var dose = new Dose(type, number);

        // Assert
        dose.Type.ShouldBe(type);
        dose.Number.ShouldBe(number);
    }

    [Theory]
    [Trait("RN", "RN08")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Reject_Dose_When_Number_Is_Below_One(int number)
    {
        // Arrange
        const DoseType type = DoseType.Primary;

        // Act
        var act = () => new Dose(type, number);

        // Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(act);
        exception.ParamName.ShouldBe(nameof(number));
    }

    [Fact]
    [Trait("RN", "RN08")]
    public void Should_Reject_Dose_When_Type_Is_Not_Defined()
    {
        // Arrange
        const DoseType type = (DoseType)99;

        // Act
        var act = () => new Dose(type, 1);

        // Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(act);
        exception.ParamName.ShouldBe(nameof(type));
    }

    [Fact]
    [Trait("RN", "RN08")]
    public void Should_Be_Equal_When_Type_And_Number_Are_Equal()
    {
        // Arrange
        var dose = new Dose(DoseType.Primary, 2);

        // Act
        var same = new Dose(DoseType.Primary, 2);

        // Assert
        same.ShouldBe(dose);
        same.ShouldNotBe(new Dose(DoseType.Booster, 2));
        same.ShouldNotBe(new Dose(DoseType.Primary, 3));
    }

    [Fact]
    [Trait("RN", "RN10")]
    public void Should_Order_Primary_Doses_Before_Boosters_When_Sorting()
    {
        // Arrange
        Dose[] doses =
        [
            new(DoseType.Booster, 2),
            new(DoseType.Primary, 2),
            new(DoseType.Booster, 1),
            new(DoseType.Primary, 1),
        ];

        // Act
        var ordered = doses.Order();

        // Assert
        ordered.ShouldBe(
        [
            new(DoseType.Primary, 1),
            new(DoseType.Primary, 2),
            new(DoseType.Booster, 1),
            new(DoseType.Booster, 2),
        ]);
    }

    [Fact]
    [Trait("RN", "RN10")]
    public void Should_Come_Before_The_First_Booster_When_Dose_Is_The_Last_Primary_Dose()
    {
        // Arrange
        var lastPrimaryDose = new Dose(DoseType.Primary, 10);
        var firstBooster = new Dose(DoseType.Booster, 1);

        // Act
        var comparison = lastPrimaryDose.CompareTo(firstBooster);

        // Assert
        comparison.ShouldBeLessThan(0);
    }
}

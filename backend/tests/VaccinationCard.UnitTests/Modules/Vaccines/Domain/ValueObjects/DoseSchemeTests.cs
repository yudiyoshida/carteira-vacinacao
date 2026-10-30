using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;
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

    [Theory]
    [Trait("RN", "RN09")]
    [InlineData(DoseType.Primary, 1)]
    [InlineData(DoseType.Primary, 3)]
    [InlineData(DoseType.Booster, 1)]
    [InlineData(DoseType.Booster, 2)]
    public void Should_Contain_Dose_When_Number_Is_Within_The_Count_Of_Its_Type(DoseType type, int number)
    {
        // Arrange
        var scheme = new DoseScheme(3, 2);

        // Act
        var contains = scheme.Contains(new Dose(type, number));

        // Assert
        contains.ShouldBeTrue();
    }

    [Theory]
    [Trait("RN", "RN09")]
    [InlineData(DoseType.Primary, 4)]
    [InlineData(DoseType.Booster, 3)]
    public void Should_Not_Contain_Dose_When_Number_Is_Above_The_Count_Of_Its_Type(DoseType type, int number)
    {
        // Arrange
        var scheme = new DoseScheme(3, 2);

        // Act
        var contains = scheme.Contains(new Dose(type, number));

        // Assert
        contains.ShouldBeFalse();
    }

    [Fact]
    [Trait("RN", "RN09")]
    public void Should_Not_Contain_Booster_When_Scheme_Has_No_Boosters()
    {
        // Arrange
        var scheme = new DoseScheme(3, 0);

        // Act
        var contains = scheme.Contains(new Dose(DoseType.Booster, 1));

        // Assert
        contains.ShouldBeFalse();
    }

    [Fact]
    [Trait("RN", "RN10")]
    public void Should_List_Primary_Doses_Then_Boosters_When_Ordering_The_Doses()
    {
        // Arrange
        var scheme = new DoseScheme(2, 2);

        // Act
        var doses = scheme.OrderedDoses();

        // Assert
        doses.ShouldBe(
        [
            new(DoseType.Primary, 1),
            new(DoseType.Primary, 2),
            new(DoseType.Booster, 1),
            new(DoseType.Booster, 2),
        ]);
    }

    [Fact]
    [Trait("RN", "RN10")]
    public void Should_List_A_Single_Dose_When_Scheme_Has_One_Primary_Dose_And_No_Boosters()
    {
        // Arrange
        var scheme = new DoseScheme(1, 0);

        // Act
        var doses = scheme.OrderedDoses();

        // Assert
        doses.ShouldBe([new(DoseType.Primary, 1)]);
    }

    // The registered doses are a prefix of the sequence, so their count points to the next one.
    [Theory]
    [Trait("RN", "RN10")]
    [InlineData(0, DoseType.Primary, 1)]
    [InlineData(1, DoseType.Primary, 2)]
    [InlineData(2, DoseType.Booster, 1)]
    [InlineData(3, DoseType.Booster, 2)]
    public void Should_Expect_The_Dose_At_The_Count_When_Scheme_Has_Doses_Left(int registeredDoses, DoseType type, int number)
    {
        // Arrange
        var scheme = new DoseScheme(2, 2);

        // Act
        var nextDose = scheme.NextExpectedDose(registeredDoses);

        // Assert
        nextDose.ShouldBe(new Dose(type, number));
    }

    [Fact]
    [Trait("RN", "RN10")]
    public void Should_Expect_No_Dose_When_Registered_Doses_Reach_The_Scheme_Total()
    {
        // Arrange
        var scheme = new DoseScheme(2, 2);
        const int registeredDoses = 4;

        // Act
        var nextDose = scheme.NextExpectedDose(registeredDoses);

        // Assert
        nextDose.ShouldBeNull();
    }

    [Fact]
    public void Should_Reject_Count_When_Registered_Doses_Are_Negative()
    {
        // Arrange
        var scheme = new DoseScheme(2, 2);
        const int registeredDoses = -1;

        // Act
        var act = () => scheme.NextExpectedDose(registeredDoses);

        // Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(act);
        exception.ParamName.ShouldBe(nameof(registeredDoses));
    }
}

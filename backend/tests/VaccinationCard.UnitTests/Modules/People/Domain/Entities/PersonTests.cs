using VaccinationCard.Api.Modules.People.Domain.Entities;
using VaccinationCard.Api.Modules.People.Domain.Factories;
using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;
using VaccinationCard.Api.Modules.Vaccines.Domain.ValueObjects;
using VaccinationCard.Api.Shared.Errors;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.UnitTests.Modules.People.Domain.Entities;

public sealed class PersonTests
{
    private static readonly DateOnly Today = new(2026, 10, 28);
    private static readonly DateOnly Tomorrow = Today.AddDays(1);
    private static readonly Guid VaccineId = Guid.CreateVersion7();
    private static readonly DoseScheme ThreeDoses = new(3, 0);
    private static readonly DoseScheme TwoDosesAndTwoBoosters = new(2, 2);
    private static readonly Dose FirstDose = new(DoseType.Primary, 1);
    private static readonly Dose SecondDose = new(DoseType.Primary, 2);
    private static readonly Dose ThirdDose = new(DoseType.Primary, 3);
    private static readonly Dose FirstBooster = new(DoseType.Booster, 1);
    private static readonly Dose SecondBooster = new(DoseType.Booster, 2);

    private readonly Person _sut = PersonFactory.Create("Maria Silva", Cpf.Create("12345678909"));

    public static TheoryData<DateOnly, string> DatesOutOfTheLimits => new()
    {
        { Tomorrow, "vaccination.applied_on_in_future" },
        { new DateOnly(1899, 12, 31), "vaccination.applied_on_before_minimum" },
    };

    [Fact]
    [Trait("RN", "RN10")]
    public void Should_Register_Vaccination_When_Dose_Is_The_Next_Expected()
    {
        // Arrange
        var appliedOn = Today.AddDays(-30);

        // Act
        var vaccination = _sut.RegisterVaccination(VaccineId, ThreeDoses, FirstDose, appliedOn, Today);

        // Assert
        vaccination.Id.Version.ShouldBe(7);
        vaccination.VaccineId.ShouldBe(VaccineId);
        vaccination.Dose.ShouldBe(FirstDose);
        vaccination.AppliedOn.ShouldBe(appliedOn);
        _sut.Vaccinations.ShouldHaveSingleItem().ShouldBeSameAs(vaccination);
    }

    [Fact]
    [Trait("RN", "RN09")]
    public void Should_Reject_Dose_When_Primary_Number_Is_Above_The_Scheme()
    {
        // Arrange
        var fourthDose = new Dose(DoseType.Primary, 4);

        // Act
        var act = () => _sut.RegisterVaccination(VaccineId, ThreeDoses, fourthDose, Today, Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.dose_outside_scheme");
    }

    [Fact]
    [Trait("RN", "RN09")]
    public void Should_Reject_Booster_When_Scheme_Has_No_Boosters()
    {
        // Arrange
        RegisterDose(ThreeDoses, FirstDose, Today.AddDays(-90));
        RegisterDose(ThreeDoses, SecondDose, Today.AddDays(-60));
        RegisterDose(ThreeDoses, ThirdDose, Today.AddDays(-30));

        // Act
        var act = () => _sut.RegisterVaccination(VaccineId, ThreeDoses, FirstBooster, Today, Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.dose_outside_scheme");
    }

    [Fact]
    [Trait("RN", "RN10")]
    [Trait("RN", "RN11")]
    public void Should_Reject_Dose_As_Already_Registered_When_It_Is_Also_Out_Of_Sequence()
    {
        // Arrange
        RegisterDose(ThreeDoses, FirstDose, Today.AddDays(-60));
        RegisterDose(ThreeDoses, SecondDose, Today.AddDays(-30));

        // Act
        var act = () => _sut.RegisterVaccination(VaccineId, ThreeDoses, FirstDose, Today, Today);

        // Assert
        var exception = Should.Throw<ConflictException>(act);
        exception.Code.ShouldBe("vaccination.dose_already_registered");
    }

    // A person read from the database brings the registered doses, and the rules count them.
    [Fact]
    [Trait("RN", "RN11")]
    public void Should_Reject_Dose_When_Loaded_Person_Already_Has_It()
    {
        // Arrange
        var registered = PersonFactory.LoadVaccination(Guid.CreateVersion7(), VaccineId, FirstDose, Today.AddDays(-30));
        var person = PersonFactory.Load(Guid.CreateVersion7(), "Maria Silva", Cpf.Create("12345678909"), [registered]);

        // Act
        var act = () => person.RegisterVaccination(VaccineId, ThreeDoses, FirstDose, Today, Today);

        // Assert
        var exception = Should.Throw<ConflictException>(act);
        exception.Code.ShouldBe("vaccination.dose_already_registered");
    }

    [Fact]
    [Trait("RN", "RN10")]
    public void Should_Reject_Dose_When_Previous_Dose_Is_Missing()
    {
        // Arrange
        var appliedOn = Today;

        // Act
        var act = () => _sut.RegisterVaccination(VaccineId, ThreeDoses, SecondDose, appliedOn, Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.dose_out_of_sequence");
    }

    [Fact]
    [Trait("RN", "RN10")]
    public void Should_Reject_Booster_When_Primary_Series_Is_Incomplete()
    {
        // Arrange
        RegisterDose(TwoDosesAndTwoBoosters, FirstDose, Today.AddDays(-30));

        // Act
        var act = () => _sut.RegisterVaccination(VaccineId, TwoDosesAndTwoBoosters, FirstBooster, Today, Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.dose_out_of_sequence");
    }

    [Fact]
    [Trait("RN", "RN10")]
    public void Should_Reject_Second_Booster_When_First_Booster_Is_Missing()
    {
        // Arrange
        RegisterDose(TwoDosesAndTwoBoosters, FirstDose, Today.AddDays(-60));
        RegisterDose(TwoDosesAndTwoBoosters, SecondDose, Today.AddDays(-30));

        // Act
        var act = () => _sut.RegisterVaccination(VaccineId, TwoDosesAndTwoBoosters, SecondBooster, Today, Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.dose_out_of_sequence");
    }

    [Fact]
    [Trait("RN", "RN10")]
    public void Should_Register_Booster_When_Primary_Series_Is_Complete()
    {
        // Arrange
        RegisterDose(TwoDosesAndTwoBoosters, FirstDose, Today.AddDays(-60));
        RegisterDose(TwoDosesAndTwoBoosters, SecondDose, Today.AddDays(-30));

        // Act
        var vaccination = _sut.RegisterVaccination(VaccineId, TwoDosesAndTwoBoosters, FirstBooster, Today, Today);

        // Assert
        vaccination.Dose.ShouldBe(FirstBooster);
        _sut.Vaccinations.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("RN", "RN12")]
    public void Should_Reject_Date_When_It_Is_In_The_Future()
    {
        // Arrange
        var appliedOn = Tomorrow;

        // Act
        var act = () => _sut.RegisterVaccination(VaccineId, ThreeDoses, FirstDose, appliedOn, Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.applied_on_in_future");
    }

    [Fact]
    [Trait("RN", "RN12")]
    public void Should_Register_Vaccination_When_Date_Is_Today()
    {
        // Arrange
        var appliedOn = Today;

        // Act
        var vaccination = _sut.RegisterVaccination(VaccineId, ThreeDoses, FirstDose, appliedOn, Today);

        // Assert
        vaccination.AppliedOn.ShouldBe(appliedOn);
    }

    [Fact]
    [Trait("RN", "RN12")]
    public void Should_Reject_Date_When_It_Is_Before_The_Minimum()
    {
        // Arrange
        var appliedOn = new DateOnly(1899, 12, 31);

        // Act
        var act = () => _sut.RegisterVaccination(VaccineId, ThreeDoses, FirstDose, appliedOn, Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.applied_on_before_minimum");
    }

    [Fact]
    [Trait("RN", "RN12")]
    public void Should_Register_Vaccination_When_Date_Is_The_Minimum()
    {
        // Arrange
        var appliedOn = new DateOnly(1900, 1, 1);

        // Act
        var vaccination = _sut.RegisterVaccination(VaccineId, ThreeDoses, FirstDose, appliedOn, Today);

        // Assert
        vaccination.AppliedOn.ShouldBe(appliedOn);
    }

    [Theory]
    [Trait("RN", "RN13")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Reject_Date_When_It_Is_Not_After_The_Previous_Dose(int daysAfterPrevious)
    {
        // Arrange
        var previousAppliedOn = Today.AddDays(-30);
        RegisterDose(ThreeDoses, FirstDose, previousAppliedOn);
        var appliedOn = previousAppliedOn.AddDays(daysAfterPrevious);

        // Act
        var act = () => _sut.RegisterVaccination(VaccineId, ThreeDoses, SecondDose, appliedOn, Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.applied_on_not_after_previous");
    }

    [Fact]
    [Trait("RN", "RN13")]
    public void Should_Register_Vaccination_When_Date_Is_The_Day_After_The_Previous_Dose()
    {
        // Arrange
        var previousAppliedOn = Today.AddDays(-30);
        RegisterDose(ThreeDoses, FirstDose, previousAppliedOn);
        var appliedOn = previousAppliedOn.AddDays(1);

        // Act
        var vaccination = _sut.RegisterVaccination(VaccineId, ThreeDoses, SecondDose, appliedOn, Today);

        // Assert
        vaccination.AppliedOn.ShouldBe(appliedOn);
    }

    [Fact]
    [Trait("RN", "RN13")]
    public void Should_Reject_First_Booster_When_Date_Is_Not_After_The_Last_Primary_Dose()
    {
        // Arrange
        RegisterDose(TwoDosesAndTwoBoosters, FirstDose, Today.AddDays(-60));
        RegisterDose(TwoDosesAndTwoBoosters, SecondDose, Today.AddDays(-30));
        var appliedOn = Today.AddDays(-45);

        // Act
        var act = () => _sut.RegisterVaccination(VaccineId, TwoDosesAndTwoBoosters, FirstBooster, appliedOn, Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.applied_on_not_after_previous");
    }

    [Fact]
    [Trait("RN", "RN09")]
    [Trait("RN", "RN12")]
    public void Should_Report_Dose_Outside_Scheme_When_Date_Is_Also_In_The_Future()
    {
        // Arrange
        var fourthDose = new Dose(DoseType.Primary, 4);

        // Act
        var act = () => _sut.RegisterVaccination(VaccineId, ThreeDoses, fourthDose, Tomorrow, Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.dose_outside_scheme");
    }

    [Fact]
    [Trait("RN", "RN10")]
    [Trait("RN", "RN12")]
    public void Should_Report_Dose_Out_Of_Sequence_When_Date_Is_Also_In_The_Future()
    {
        // Arrange
        var appliedOn = Tomorrow;

        // Act
        var act = () => _sut.RegisterVaccination(VaccineId, ThreeDoses, SecondDose, appliedOn, Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.dose_out_of_sequence");
    }

    [Fact]
    [Trait("RN", "RN12")]
    [Trait("RN", "RN13")]
    public void Should_Report_Future_Date_When_Date_Is_Also_Not_After_The_Previous_Dose()
    {
        // Arrange
        // Registered with tomorrow as "today", the first dose makes tomorrow both a future date and not after it.
        _sut.RegisterVaccination(VaccineId, ThreeDoses, FirstDose, Tomorrow, Tomorrow);

        // Act
        var act = () => _sut.RegisterVaccination(VaccineId, ThreeDoses, SecondDose, Tomorrow, Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.applied_on_in_future");
    }

    [Fact]
    [Trait("RN", "RN10")]
    [Trait("RN", "RN11")]
    [Trait("RN", "RN13")]
    public void Should_Register_First_Dose_When_Another_Vaccine_Already_Has_Later_Doses()
    {
        // Arrange
        var otherVaccineId = Guid.CreateVersion7();
        RegisterDose(ThreeDoses, FirstDose, Today.AddDays(-20));
        RegisterDose(ThreeDoses, SecondDose, Today.AddDays(-10));
        var appliedOn = Today.AddDays(-30);

        // Act
        var vaccination = _sut.RegisterVaccination(otherVaccineId, ThreeDoses, FirstDose, appliedOn, Today);

        // Assert
        vaccination.VaccineId.ShouldBe(otherVaccineId);
        _sut.NextExpectedDose(otherVaccineId, ThreeDoses).ShouldBe(SecondDose);
        _sut.NextExpectedDose(VaccineId, ThreeDoses).ShouldBe(ThirdDose);
    }

    [Fact]
    [Trait("RN", "RN10")]
    public void Should_Expect_The_First_Dose_When_Vaccine_Has_No_Vaccination()
    {
        // Arrange
        var otherVaccineId = Guid.CreateVersion7();
        RegisterDose(ThreeDoses, FirstDose, Today);

        // Act
        var nextDose = _sut.NextExpectedDose(otherVaccineId, ThreeDoses);

        // Assert
        nextDose.ShouldBe(FirstDose);
    }

    [Fact]
    [Trait("RN", "RN10")]
    public void Should_Expect_The_Next_Primary_Dose_When_Primary_Series_Is_In_Progress()
    {
        // Arrange
        RegisterDose(ThreeDoses, FirstDose, Today);

        // Act
        var nextDose = _sut.NextExpectedDose(VaccineId, ThreeDoses);

        // Assert
        nextDose.ShouldBe(SecondDose);
    }

    [Fact]
    [Trait("RN", "RN10")]
    public void Should_Expect_The_First_Booster_When_Primary_Series_Is_Complete()
    {
        // Arrange
        RegisterDose(TwoDosesAndTwoBoosters, FirstDose, Today.AddDays(-30));
        RegisterDose(TwoDosesAndTwoBoosters, SecondDose, Today);

        // Act
        var nextDose = _sut.NextExpectedDose(VaccineId, TwoDosesAndTwoBoosters);

        // Assert
        nextDose.ShouldBe(FirstBooster);
    }

    [Fact]
    [Trait("RN", "RN10")]
    public void Should_Expect_No_Dose_When_Scheme_Is_Complete()
    {
        // Arrange
        var oneDoseAndOneBooster = new DoseScheme(1, 1);
        RegisterDose(oneDoseAndOneBooster, FirstDose, Today.AddDays(-30));
        RegisterDose(oneDoseAndOneBooster, FirstBooster, Today);

        // Act
        var nextDose = _sut.NextExpectedDose(VaccineId, oneDoseAndOneBooster);

        // Assert
        nextDose.ShouldBeNull();
    }

    [Fact]
    public void Should_Throw_Not_Found_When_Correcting_A_Vaccination_That_Does_Not_Exist()
    {
        // Arrange
        RegisterDose(ThreeDoses, FirstDose, Today.AddDays(-30));
        var vaccinationId = Guid.CreateVersion7();

        // Act
        var act = () => _sut.CorrectVaccinationDate(vaccinationId, Today, Today);

        // Assert
        var exception = Should.Throw<NotFoundException>(act);
        exception.Code.ShouldBe("vaccination.not_found");
    }

    // The second dose sits between the first, 90 days ago, and the third, 30 days ago.
    [Theory]
    [Trait("RN", "RN14")]
    [InlineData(-89)]
    [InlineData(-31)]
    public void Should_Correct_Only_The_Date_When_New_Date_Is_Between_The_Neighbouring_Doses(int daysFromToday)
    {
        // Arrange
        RegisterDose(ThreeDoses, FirstDose, Today.AddDays(-90));
        var second = RegisterDose(ThreeDoses, SecondDose, Today.AddDays(-60));
        RegisterDose(ThreeDoses, ThirdDose, Today.AddDays(-30));
        var appliedOn = Today.AddDays(daysFromToday);

        // Act
        _sut.CorrectVaccinationDate(second.Id, appliedOn, Today);

        // Assert
        var corrected = _sut.Vaccinations.Single(vaccination => vaccination.Id == second.Id);
        corrected.AppliedOn.ShouldBe(appliedOn);
        corrected.VaccineId.ShouldBe(VaccineId);
        corrected.Dose.ShouldBe(SecondDose);
    }

    // The second dose sits between the first, 90 days ago, and the third, 30 days ago.
    [Theory]
    [Trait("RN", "RN14")]
    [InlineData(-91)]
    [InlineData(-90)]
    [InlineData(-30)]
    [InlineData(-29)]
    public void Should_Reject_Correction_When_New_Date_Is_Not_Between_The_Neighbouring_Doses(int daysFromToday)
    {
        // Arrange
        RegisterDose(ThreeDoses, FirstDose, Today.AddDays(-90));
        var second = RegisterDose(ThreeDoses, SecondDose, Today.AddDays(-60));
        RegisterDose(ThreeDoses, ThirdDose, Today.AddDays(-30));

        // Act
        var act = () => _sut.CorrectVaccinationDate(second.Id, Today.AddDays(daysFromToday), Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.applied_on_out_of_order");
    }

    // The first dose has no previous dose, so only the next one limits the new date.
    [Theory]
    [Trait("RN", "RN14")]
    [InlineData(-365)]
    [InlineData(-31)]
    public void Should_Correct_First_Dose_When_New_Date_Is_Before_The_Next_Dose(int daysFromToday)
    {
        // Arrange
        var first = RegisterDose(ThreeDoses, FirstDose, Today.AddDays(-60));
        RegisterDose(ThreeDoses, SecondDose, Today.AddDays(-30));
        var appliedOn = Today.AddDays(daysFromToday);

        // Act
        _sut.CorrectVaccinationDate(first.Id, appliedOn, Today);

        // Assert
        _sut.Vaccinations.Single(vaccination => vaccination.Id == first.Id).AppliedOn.ShouldBe(appliedOn);
    }

    [Fact]
    [Trait("RN", "RN14")]
    public void Should_Reject_Correction_Of_First_Dose_When_New_Date_Is_The_Date_Of_The_Next_Dose()
    {
        // Arrange
        var first = RegisterDose(ThreeDoses, FirstDose, Today.AddDays(-60));
        RegisterDose(ThreeDoses, SecondDose, Today.AddDays(-30));

        // Act
        var act = () => _sut.CorrectVaccinationDate(first.Id, Today.AddDays(-30), Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.applied_on_out_of_order");
    }

    // The last dose has no next dose, so only the previous one and today limit the new date.
    [Theory]
    [Trait("RN", "RN12")]
    [Trait("RN", "RN14")]
    [InlineData(-59)]
    [InlineData(0)]
    public void Should_Correct_Last_Dose_When_New_Date_Is_After_The_Previous_Dose_And_Not_After_Today(int daysFromToday)
    {
        // Arrange
        RegisterDose(ThreeDoses, FirstDose, Today.AddDays(-60));
        var last = RegisterDose(ThreeDoses, SecondDose, Today.AddDays(-30));
        var appliedOn = Today.AddDays(daysFromToday);

        // Act
        _sut.CorrectVaccinationDate(last.Id, appliedOn, Today);

        // Assert
        _sut.Vaccinations.Single(vaccination => vaccination.Id == last.Id).AppliedOn.ShouldBe(appliedOn);
    }

    [Fact]
    [Trait("RN", "RN14")]
    public void Should_Reject_Correction_Of_Last_Dose_When_New_Date_Is_The_Date_Of_The_Previous_Dose()
    {
        // Arrange
        RegisterDose(ThreeDoses, FirstDose, Today.AddDays(-60));
        var last = RegisterDose(ThreeDoses, SecondDose, Today.AddDays(-30));

        // Act
        var act = () => _sut.CorrectVaccinationDate(last.Id, Today.AddDays(-60), Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.applied_on_out_of_order");
    }

    [Fact]
    [Trait("RN", "RN12")]
    [Trait("RN", "RN14")]
    public void Should_Correct_Date_To_The_Minimum_When_Dose_Has_No_Neighbours()
    {
        // Arrange
        var vaccination = RegisterDose(ThreeDoses, FirstDose, Today.AddDays(-30));
        var appliedOn = new DateOnly(1900, 1, 1);

        // Act
        _sut.CorrectVaccinationDate(vaccination.Id, appliedOn, Today);

        // Assert
        _sut.Vaccinations.ShouldHaveSingleItem().AppliedOn.ShouldBe(appliedOn);
    }

    [Theory]
    [Trait("RN", "RN12")]
    [MemberData(nameof(DatesOutOfTheLimits))]
    public void Should_Reject_Correction_When_New_Date_Is_Out_Of_The_Limits(DateOnly appliedOn, string code)
    {
        // Arrange
        var vaccination = RegisterDose(ThreeDoses, FirstDose, Today.AddDays(-30));

        // Act
        var act = () => _sut.CorrectVaccinationDate(vaccination.Id, appliedOn, Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe(code);
    }

    // Each date is also out of order, between the first and the third dose: RN12 comes first.
    [Theory]
    [Trait("RN", "RN12")]
    [Trait("RN", "RN14")]
    [MemberData(nameof(DatesOutOfTheLimits))]
    public void Should_Report_The_Date_Limit_When_New_Date_Is_Also_Out_Of_Order(DateOnly appliedOn, string code)
    {
        // Arrange
        RegisterDose(ThreeDoses, FirstDose, Today.AddDays(-90));
        var second = RegisterDose(ThreeDoses, SecondDose, Today.AddDays(-60));
        RegisterDose(ThreeDoses, ThirdDose, Today.AddDays(-30));

        // Act
        var act = () => _sut.CorrectVaccinationDate(second.Id, appliedOn, Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe(code);
    }

    [Fact]
    [Trait("RN", "RN14")]
    public void Should_Reject_Correction_When_Last_Primary_Dose_Moves_To_The_Date_Of_The_First_Booster()
    {
        // Arrange
        RegisterDose(TwoDosesAndTwoBoosters, FirstDose, Today.AddDays(-90));
        var lastPrimary = RegisterDose(TwoDosesAndTwoBoosters, SecondDose, Today.AddDays(-60));
        RegisterDose(TwoDosesAndTwoBoosters, FirstBooster, Today.AddDays(-30));

        // Act
        var act = () => _sut.CorrectVaccinationDate(lastPrimary.Id, Today.AddDays(-30), Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.applied_on_out_of_order");
    }

    [Fact]
    [Trait("RN", "RN14")]
    public void Should_Reject_Correction_When_First_Booster_Moves_To_The_Date_Of_The_Last_Primary_Dose()
    {
        // Arrange
        RegisterDose(TwoDosesAndTwoBoosters, FirstDose, Today.AddDays(-90));
        RegisterDose(TwoDosesAndTwoBoosters, SecondDose, Today.AddDays(-60));
        var firstBooster = RegisterDose(TwoDosesAndTwoBoosters, FirstBooster, Today.AddDays(-30));

        // Act
        var act = () => _sut.CorrectVaccinationDate(firstBooster.Id, Today.AddDays(-60), Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.applied_on_out_of_order");
    }

    // Each vaccine has its own sequence: the new date falls after the doses of the other vaccine.
    [Fact]
    [Trait("RN", "RN14")]
    public void Should_Correct_Date_When_New_Date_Is_Out_Of_Order_Only_For_Another_Vaccine()
    {
        // Arrange
        var otherVaccineId = Guid.CreateVersion7();
        var vaccination = RegisterDose(ThreeDoses, FirstDose, Today.AddDays(-60));
        _sut.RegisterVaccination(otherVaccineId, ThreeDoses, FirstDose, Today.AddDays(-50), Today);
        _sut.RegisterVaccination(otherVaccineId, ThreeDoses, SecondDose, Today.AddDays(-40), Today);
        var appliedOn = Today.AddDays(-30);

        // Act
        _sut.CorrectVaccinationDate(vaccination.Id, appliedOn, Today);

        // Assert
        _sut.Vaccinations.Single(item => item.Id == vaccination.Id).AppliedOn.ShouldBe(appliedOn);
    }

    // In this list, the second dose comes last: by position, it would have no next dose, and the new date would pass.
    [Fact]
    [Trait("RN", "RN14")]
    public void Should_Find_The_Neighbouring_Doses_By_The_Sequence_When_Loaded_Person_Has_Them_Out_Of_Order()
    {
        // Arrange
        var first = PersonFactory.LoadVaccination(Guid.CreateVersion7(), VaccineId, FirstDose, Today.AddDays(-90));
        var second = PersonFactory.LoadVaccination(Guid.CreateVersion7(), VaccineId, SecondDose, Today.AddDays(-60));
        var third = PersonFactory.LoadVaccination(Guid.CreateVersion7(), VaccineId, ThirdDose, Today.AddDays(-30));
        var person = PersonFactory.Load(Guid.CreateVersion7(), "Maria Silva", Cpf.Create("12345678909"), [third, first, second]);

        // Act
        var act = () => person.CorrectVaccinationDate(second.Id, Today.AddDays(-30), Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.applied_on_out_of_order");
    }

    // Sets up a dose registered before the one the test checks.
    private Vaccination RegisterDose(DoseScheme scheme, Dose dose, DateOnly appliedOn) =>
        _sut.RegisterVaccination(VaccineId, scheme, dose, appliedOn, Today);
}

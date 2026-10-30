using VaccinationCard.Api.Modules.People.Application.UseCases.RegisterVaccination;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;

namespace VaccinationCard.UnitTests.Modules.People.Application.UseCases.RegisterVaccination;

public sealed class RegisterVaccinationValidatorTests
{
    private readonly RegisterVaccinationValidator _sut = new();

    [Fact]
    public void Should_Accept_Input_When_Every_Field_Is_Valid()
    {
        // Arrange
        var input = MakeRegisterVaccinationInput();

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Reject_Input_When_Vaccine_Is_Missing()
    {
        // Arrange
        var input = MakeRegisterVaccinationInput() with { VaccineId = Guid.Empty };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(RegisterVaccinationCommand.VaccineId));
    }

    [Fact]
    [Trait("RN", "RN08")]
    public void Should_Reject_Input_When_Dose_Is_Missing()
    {
        // Arrange
        var input = MakeRegisterVaccinationInput() with { Dose = null! };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(RegisterVaccinationCommand.Dose));
    }

    [Theory]
    [Trait("RN", "RN08")]
    [InlineData(DoseType.Primary)]
    [InlineData(DoseType.Booster)]
    public void Should_Accept_Dose_When_Type_Is_Defined(DoseType type)
    {
        // Arrange
        var input = MakeRegisterVaccinationInput() with { Dose = new DoseDto(type, 1) };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    // A type missing from the request body arrives as the default value of the enum.
    [Fact]
    [Trait("RN", "RN08")]
    public void Should_Reject_Dose_When_Type_Is_Missing()
    {
        // Arrange
        var input = MakeRegisterVaccinationInput() with { Dose = new DoseDto(default, 1) };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == "Dose.Type");
    }

    [Theory]
    [Trait("RN", "RN08")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Reject_Dose_When_Number_Is_Below_One(int number)
    {
        // Arrange
        var input = MakeRegisterVaccinationInput() with { Dose = new DoseDto(DoseType.Primary, number) };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == "Dose.Number");
    }

    [Fact]
    [Trait("RN", "RN12")]
    public void Should_Reject_Input_When_Applied_On_Is_Missing()
    {
        // Arrange
        var input = MakeRegisterVaccinationInput() with { AppliedOn = default };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(RegisterVaccinationCommand.AppliedOn));
    }

    private static RegisterVaccinationCommand MakeRegisterVaccinationInput() =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15));
}

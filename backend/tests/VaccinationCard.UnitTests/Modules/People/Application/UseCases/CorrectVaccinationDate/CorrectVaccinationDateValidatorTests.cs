using VaccinationCard.Api.Modules.People.Application.UseCases.CorrectVaccinationDate;

namespace VaccinationCard.UnitTests.Modules.People.Application.UseCases.CorrectVaccinationDate;

public sealed class CorrectVaccinationDateValidatorTests
{
    private readonly CorrectVaccinationDateValidator _sut = new();

    [Fact]
    public void Should_Accept_Input_When_Applied_On_Is_Present()
    {
        // Arrange
        var input = MakeCorrectVaccinationDateInput();

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("RN", "RN12")]
    public void Should_Reject_Input_When_Applied_On_Is_Missing()
    {
        // Arrange
        var input = MakeCorrectVaccinationDateInput() with { AppliedOn = default };

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CorrectVaccinationDateCommand.AppliedOn));
    }

    private static CorrectVaccinationDateCommand MakeCorrectVaccinationDateInput() =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), new DateOnly(2020, 1, 15));
}

using FluentValidation;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.CorrectVaccinationDate;

// Checks only the form of the input: the limits and the order of the dates are rules of the aggregate (RN12, RN14).
public sealed class CorrectVaccinationDateValidator : AbstractValidator<CorrectVaccinationDateCommand>
{
    public CorrectVaccinationDateValidator()
    {
        // The applied-on date is required (RN12).
        RuleFor(x => x.AppliedOn)
            .NotEmpty()
            .WithMessage("A data de aplicação é obrigatória.");
    }
}

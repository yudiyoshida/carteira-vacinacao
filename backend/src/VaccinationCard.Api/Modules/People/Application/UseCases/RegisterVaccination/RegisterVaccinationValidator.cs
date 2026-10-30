using FluentValidation;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.RegisterVaccination;

// Checks only the form of the input: the scheme, the sequence and the dates are rules of the aggregate (RN09 to RN13).
public sealed class RegisterVaccinationValidator : AbstractValidator<RegisterVaccinationCommand>
{
    public RegisterVaccinationValidator()
    {
        RuleFor(x => x.VaccineId)
            .NotEmpty()
            .WithMessage("A vacina é obrigatória.");

        // A dose is a type and a number within the type, from 1 (RN08).
        RuleFor(x => x.Dose)
            .NotNull()
            .WithMessage("A dose é obrigatória.")
            .ChildRules(dose =>
            {
                dose.RuleFor(x => x.Type)
                    .IsInEnum()
                    .WithMessage("O tipo da dose deve ser Primary ou Booster.");

                dose.RuleFor(x => x.Number)
                    .GreaterThanOrEqualTo(1)
                    .WithMessage("O número da dose deve ser maior que zero.");
            });

        // The applied-on date is required (RN12).
        RuleFor(x => x.AppliedOn)
            .NotEmpty()
            .WithMessage("A data de aplicação é obrigatória.");
    }
}

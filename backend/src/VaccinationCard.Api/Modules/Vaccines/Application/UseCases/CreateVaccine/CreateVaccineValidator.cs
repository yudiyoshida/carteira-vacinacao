using FluentValidation;
using VaccinationCard.Api.Modules.Vaccines.Domain.ValueObjects;

namespace VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;

public sealed class CreateVaccineValidator : AbstractValidator<CreateVaccineCommand>
{
    public CreateVaccineValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => name?.Trim().Length is >= 2 and <= 100)
            .WithMessage("O nome deve ter entre 2 e 100 caracteres.");

        RuleFor(x => x.PrimaryDoses)
            .InclusiveBetween(1, DoseScheme.MaxPrimaryDoses)
            .WithMessage("A vacina deve ter de 1 a 10 doses primárias.");

        RuleFor(x => x.BoosterDoses)
            .InclusiveBetween(0, DoseScheme.MaxBoosterDoses)
            .WithMessage("A vacina deve ter de 0 a 5 doses de reforço.");
    }
}

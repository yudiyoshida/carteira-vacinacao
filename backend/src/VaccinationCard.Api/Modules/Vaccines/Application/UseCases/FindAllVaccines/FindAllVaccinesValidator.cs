using FluentValidation;
using VaccinationCard.Api.Core.Validation;

namespace VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindAllVaccines;

public sealed class FindAllVaccinesValidator : AbstractValidator<FindAllVaccinesQuery>
{
    public FindAllVaccinesValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.Size).ValidSize();
        RuleFor(x => x.Search).ValidSearch();
    }
}

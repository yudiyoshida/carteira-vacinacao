using FluentValidation;
using VaccinationCard.Api.Core.Validation;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.FindAllPeople;

public sealed class FindAllPeopleValidator : AbstractValidator<FindAllPeopleQuery>
{
    public FindAllPeopleValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.Size).ValidSize();
        RuleFor(x => x.Search).ValidSearch();
    }
}

using MediatR;
using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Shared.Pagination;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.FindAllPeople;

public sealed record FindAllPeopleQuery(int? Page, int? Size, string? Search) : IRequest<Paginated<PersonSummaryDto>>;

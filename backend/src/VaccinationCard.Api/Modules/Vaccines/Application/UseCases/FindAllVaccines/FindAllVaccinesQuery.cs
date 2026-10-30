using MediatR;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Shared.Pagination;

namespace VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindAllVaccines;

public sealed record FindAllVaccinesQuery(int? Page, int? Size, string? Search) : IRequest<Paginated<VaccineDto>>;

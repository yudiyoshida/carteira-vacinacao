using MediatR;
using VaccinationCard.Api.Modules.People.Application.Dtos;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.FindVaccinationCard;

public sealed record FindVaccinationCardQuery(Guid PersonId) : IRequest<VaccinationCardDto>;

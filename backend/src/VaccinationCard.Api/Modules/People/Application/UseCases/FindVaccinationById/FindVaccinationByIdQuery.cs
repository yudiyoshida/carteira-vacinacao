using MediatR;
using VaccinationCard.Api.Modules.People.Application.Dtos;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.FindVaccinationById;

public sealed record FindVaccinationByIdQuery(Guid PersonId, Guid VaccinationId) : IRequest<VaccinationDto>;

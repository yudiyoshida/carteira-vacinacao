using MediatR;
using VaccinationCard.Api.Modules.People.Application.Dtos;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.FindPersonById;

public sealed record FindPersonByIdQuery(Guid Id) : IRequest<PersonDto>;

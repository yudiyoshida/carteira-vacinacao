using MediatR;
using VaccinationCard.Api.Core.Messages;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.DeletePerson;

public sealed record DeletePersonCommand(Guid Id) : IRequest<SuccessMessage>;

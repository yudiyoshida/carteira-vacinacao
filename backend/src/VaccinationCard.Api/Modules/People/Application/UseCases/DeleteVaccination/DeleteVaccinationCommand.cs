using MediatR;
using VaccinationCard.Api.Core.Messages;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.DeleteVaccination;

public sealed record DeleteVaccinationCommand(Guid PersonId, Guid VaccinationId) : IRequest<SuccessMessage>;

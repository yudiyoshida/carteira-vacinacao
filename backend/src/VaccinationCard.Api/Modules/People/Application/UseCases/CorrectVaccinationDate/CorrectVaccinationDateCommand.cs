using MediatR;
using VaccinationCard.Api.Core.Messages;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.CorrectVaccinationDate;

public sealed record CorrectVaccinationDateCommand(Guid PersonId, Guid VaccinationId, DateOnly AppliedOn) : IRequest<SuccessMessage>;

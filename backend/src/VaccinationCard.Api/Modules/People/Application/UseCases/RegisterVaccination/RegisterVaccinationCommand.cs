using MediatR;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.RegisterVaccination;

public sealed record RegisterVaccinationCommand(Guid PersonId, Guid VaccineId, DoseDto Dose, DateOnly AppliedOn) : IRequest<RegisterVaccinationOutput>;

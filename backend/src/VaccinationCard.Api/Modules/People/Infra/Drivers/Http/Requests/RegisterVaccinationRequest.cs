using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;

namespace VaccinationCard.Api.Modules.People.Infra.Drivers.Http.Requests;

public sealed record RegisterVaccinationRequest(Guid VaccineId, DoseDto Dose, DateOnly AppliedOn);

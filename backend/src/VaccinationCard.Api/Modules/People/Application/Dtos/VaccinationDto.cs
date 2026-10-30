using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;

namespace VaccinationCard.Api.Modules.People.Application.Dtos;

public sealed record VaccinationDto(Guid Id, Guid VaccineId, DoseDto Dose, DateOnly AppliedOn);

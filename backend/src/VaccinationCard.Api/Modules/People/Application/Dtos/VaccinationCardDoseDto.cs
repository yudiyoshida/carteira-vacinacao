using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;

namespace VaccinationCard.Api.Modules.People.Application.Dtos;

public sealed record VaccinationCardDoseDto(Guid VaccinationId, DoseDto Dose, DateOnly AppliedOn);

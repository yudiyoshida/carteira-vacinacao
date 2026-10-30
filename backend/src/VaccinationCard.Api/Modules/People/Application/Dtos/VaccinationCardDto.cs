namespace VaccinationCard.Api.Modules.People.Application.Dtos;

// The person and one entry per vaccine with at least one vaccination (RN17, TechSpec §7.3).
public sealed record VaccinationCardDto(PersonDto Person, IReadOnlyList<VaccinationCardEntryDto> Vaccines);

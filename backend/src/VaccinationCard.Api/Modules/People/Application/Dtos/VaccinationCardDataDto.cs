namespace VaccinationCard.Api.Modules.People.Application.Dtos;

// What People stores for a vaccination card: the person and the vaccinations. The names and schemes of the vaccines
// come from Vaccines (TechSpec §4.3).
public sealed record VaccinationCardDataDto(PersonDto Person, IReadOnlyList<VaccinationDto> Vaccinations);

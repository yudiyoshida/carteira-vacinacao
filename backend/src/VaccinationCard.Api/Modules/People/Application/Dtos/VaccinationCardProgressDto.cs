namespace VaccinationCard.Api.Modules.People.Application.Dtos;

public sealed record VaccinationCardProgressDto(int PrimaryApplied, int PrimaryTotal, bool PrimarySeriesComplete, int BoostersApplied, int BoostersTotal);

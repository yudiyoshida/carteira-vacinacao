namespace VaccinationCard.Api.Modules.Vaccines.Application.Dtos;

public sealed record VaccineDto(Guid Id, string Name, int PrimaryDoses, int BoosterDoses);

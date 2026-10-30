using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;

namespace VaccinationCard.Api.Modules.Vaccines.Application.Dtos;

// Also part of the contracts of People: the dose of a vaccination and of the vaccination card (TechSpec §5.1).
public sealed record DoseDto(DoseType Type, int Number);

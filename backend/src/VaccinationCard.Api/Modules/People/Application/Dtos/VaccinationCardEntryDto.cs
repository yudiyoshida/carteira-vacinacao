using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;

namespace VaccinationCard.Api.Modules.People.Application.Dtos;

// NextExpectedDose is null when the scheme is complete; the interface suggests it when registering a dose (F07).
public sealed record VaccinationCardEntryDto(
    Guid VaccineId,
    string VaccineName,
    DoseSchemeDto Scheme,
    IReadOnlyList<VaccinationCardDoseDto> Doses,
    VaccinationCardProgressDto Progress,
    DoseDto? NextExpectedDose
);

namespace VaccinationCard.Api.Modules.Vaccines.Domain.Enums;

// Numbered from 1: a type missing from a request body arrives as 0, which the validator rejects (RN08).
public enum DoseType
{
    Primary = 1,
    Booster = 2,
}

using VaccinationCard.Api.Modules.Vaccines.Domain.ValueObjects;

namespace VaccinationCard.Api.Modules.People.Domain.Entities;

public sealed class Vaccination
{
    // The applied-on date is not before 01/01/1900 (RN12).
    public static readonly DateOnly MinAppliedOn = new(1900, 1, 1);

    internal Vaccination(Guid id, Guid vaccineId, Dose dose, DateOnly appliedOn)
    {
        Id = id;
        VaccineId = vaccineId;
        Dose = dose;
        AppliedOn = appliedOn;
    }

    public Guid Id { get; }

    public Guid VaccineId { get; }

    public Dose Dose { get; }

    public DateOnly AppliedOn { get; }
}

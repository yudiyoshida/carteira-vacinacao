using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;

namespace VaccinationCard.Api.Modules.Vaccines.Domain.ValueObjects;

// The doses a vaccine admits (RN06). The validator rejects an out-of-range input first; the guards here catch a
// programming error.
public sealed record DoseScheme
{
    public const int MaxPrimaryDoses = 10;
    public const int MaxBoosterDoses = 5;

    public DoseScheme(int primaryDoses, int boosterDoses)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(primaryDoses, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(primaryDoses, MaxPrimaryDoses);
        ArgumentOutOfRangeException.ThrowIfNegative(boosterDoses);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(boosterDoses, MaxBoosterDoses);

        PrimaryDoses = primaryDoses;
        BoosterDoses = boosterDoses;
    }

    public int PrimaryDoses { get; }

    public int BoosterDoses { get; }

    // A dose goes from 1 up to the count of its type, so a scheme without boosters takes no booster (RN09).
    public bool Contains(Dose dose)
    {
        var count = dose.Type == DoseType.Primary ? PrimaryDoses : BoosterDoses;
        return dose.Number <= count;
    }

    // The dose sequence (RN10): the primary doses in order, then the boosters in order.
    public IReadOnlyList<Dose> OrderedDoses()
    {
        var primaryDoses = Enumerable.Range(1, PrimaryDoses).Select(number => new Dose(DoseType.Primary, number));
        var boosterDoses = Enumerable.Range(1, BoosterDoses).Select(number => new Dose(DoseType.Booster, number));
        return [.. primaryDoses, .. boosterDoses];
    }
}

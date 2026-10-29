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
}

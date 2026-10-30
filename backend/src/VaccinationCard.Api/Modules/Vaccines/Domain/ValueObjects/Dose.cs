using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;

namespace VaccinationCard.Api.Modules.Vaccines.Domain.ValueObjects;

// A position in the scheme of a vaccine: the type and the number within the type, from 1 (RN08). The validator rejects
// an invalid input first; the guards here catch a programming error.
public sealed record Dose : IComparable<Dose>
{
    public Dose(DoseType type, int number)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);

        Type = type;
        Number = number;
    }

    public DoseType Type { get; }

    public int Number { get; }

    // The sequence order (RN10): the primary doses come before the boosters, and each type goes by number.
    public int CompareTo(Dose? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (Type != other.Type)
        {
            return Type == DoseType.Primary ? -1 : 1;
        }

        return Number.CompareTo(other.Number);
    }
}

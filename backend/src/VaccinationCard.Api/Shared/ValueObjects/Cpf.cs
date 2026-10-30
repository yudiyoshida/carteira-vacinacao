namespace VaccinationCard.Api.Shared.ValueObjects;

// A CPF with valid check digits, kept as its 11 digits (RN02). The validator rejects an invalid input first; the guard
// in Create catches a programming error.
public sealed record Cpf
{
    private const int DigitCount = 11;

    private Cpf(string value)
    {
        Value = value;
    }

    public string Value { get; }

    // The lookup of a person and the vaccination card show the whole CPF (RN26).
    public string Formatted => $"{Value[..3]}.{Value[3..6]}.{Value[6..9]}-{Value[9..]}";

    // The lists show only the middle digits (RN26).
    public string Masked => $"***.{Value[3..6]}.{Value[6..9]}-**";

    public static bool IsValid(string? value)
    {
        return Digits(value) is { } digits && HasValidCheckDigits(digits);
    }

    public static Cpf Create(string value)
    {
        var digits = Digits(value);
        if (digits is null || !HasValidCheckDigits(digits))
        {
            throw new ArgumentException("O CPF informado é inválido.", nameof(value));
        }

        return new Cpf(digits);
    }

    // A CPF that reaches a log or a message by accident goes out masked (RN25).
    public override string ToString()
    {
        return Masked;
    }

    // The CPF comes with or without the mask: dots, hyphens and spaces are dropped anywhere, and 11 digits must remain
    // (TechSpec §5.2).
    private static string? Digits(string? value)
    {
        var digits = value?.Where(character => character is not ('.' or '-') && !char.IsWhiteSpace(character)).ToArray();
        return digits is { Length: DigitCount } && digits.All(char.IsAsciiDigit) ? new string(digits) : null;
    }

    // A single repeated digit passes the check digits, so it is rejected apart.
    private static bool HasValidCheckDigits(string digits) =>
        digits.Distinct().Count() > 1
        && CheckDigit(digits, 9) == digits[9] - '0'
        && CheckDigit(digits, 10) == digits[10] - '0';

    // Each check digit comes from the digits before it, weighted from count + 1 down to 2; a remainder of 10 becomes 0.
    private static int CheckDigit(string digits, int count)
    {
        var sum = 0;
        for (var index = 0; index < count; index++)
        {
            sum += (digits[index] - '0') * (count + 1 - index);
        }

        var remainder = sum * 10 % 11;
        return remainder == 10 ? 0 : remainder;
    }
}

namespace VaccinationCard.Api.Infra.Database;

// Turns a search term into an ILIKE pattern that finds the term as text, anywhere in the value (TechSpec §6.2).
public static class LikePattern
{
    public const string EscapeCharacter = @"\";

    // The escape character goes first, so the escapes added before % and _ are not doubled.
    public static string Contains(string term)
    {
        var escaped = term
            .Replace(EscapeCharacter, EscapeCharacter + EscapeCharacter)
            .Replace("%", EscapeCharacter + "%")
            .Replace("_", EscapeCharacter + "_");

        return $"%{escaped}%";
    }
}

namespace VaccinationCard.Api.Core.Messages;

// The body of an edit or a removal: 200 { message } (TechSpec §7.1).
public sealed record SuccessMessage(string Message);

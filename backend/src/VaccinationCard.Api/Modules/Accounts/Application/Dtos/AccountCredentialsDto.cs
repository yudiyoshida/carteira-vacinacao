namespace VaccinationCard.Api.Modules.Accounts.Application.Dtos;

// Stays inside Accounts: the password hash never leaves the module (TechSpec §5.3).
public sealed record AccountCredentialsDto(Guid Id, string Email, string PasswordHash);

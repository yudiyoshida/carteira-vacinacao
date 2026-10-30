namespace VaccinationCard.Api.Modules.Accounts.Infra.Driven.Persistence.EfCore;

public sealed class AccountRecord
{
    public required Guid Id { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

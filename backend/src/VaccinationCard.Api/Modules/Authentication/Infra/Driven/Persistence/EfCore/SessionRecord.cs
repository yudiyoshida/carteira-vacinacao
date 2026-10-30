namespace VaccinationCard.Api.Modules.Authentication.Infra.Driven.Persistence.EfCore;

public sealed class SessionRecord
{
    public required Guid AccountId { get; set; }

    public required string RefreshTokenHash { get; set; }

    public required DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

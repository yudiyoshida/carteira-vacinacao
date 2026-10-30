namespace VaccinationCard.Api.Modules.Authentication.Application.Dtos;

public sealed record SessionDto(Guid AccountId, string RefreshTokenHash, DateTimeOffset ExpiresAt);

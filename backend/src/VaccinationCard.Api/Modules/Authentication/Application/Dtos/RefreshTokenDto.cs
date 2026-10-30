namespace VaccinationCard.Api.Modules.Authentication.Application.Dtos;

public sealed record RefreshTokenDto(string Token, DateTimeOffset ExpiresAt);

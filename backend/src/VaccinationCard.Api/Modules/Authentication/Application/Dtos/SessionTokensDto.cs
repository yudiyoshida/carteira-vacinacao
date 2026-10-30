namespace VaccinationCard.Api.Modules.Authentication.Application.Dtos;

// What the controller needs to answer: the access token for the body and the refresh token for the cookie.
public sealed record SessionTokensDto(string AccessToken, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);

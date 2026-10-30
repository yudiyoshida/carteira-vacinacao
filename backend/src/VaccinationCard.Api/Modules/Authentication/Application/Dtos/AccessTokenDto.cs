namespace VaccinationCard.Api.Modules.Authentication.Application.Dtos;

// The body of the login and of the renewal: the refresh token goes only in the cookie (ADR-0007).
public sealed record AccessTokenDto(string AccessToken);

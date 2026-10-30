using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinationCard.Api.Modules.Authentication.Application.Dtos;
using VaccinationCard.Api.Modules.Authentication.Application.UseCases.SignIn;

namespace VaccinationCard.Api.Modules.Authentication.Infra.Drivers.Http;

[ApiController]
[Route("api/v1/auth")]
[Tags("Autenticação")]
public sealed class AuthController(ISender sender, IConfiguration configuration) : ControllerBase
{
    private const string RefreshTokenCookie = "refresh_token";
    private const string RefreshTokenCookiePath = "/api/v1/auth";

    [HttpPost("login")]
    [AllowAnonymous]
    [EndpointSummary("Abre a sessão do operador")]
    [ProducesResponseType<AccessTokenDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<AccessTokenDto>> Login(SignInCommand command, CancellationToken cancellationToken)
    {
        var tokens = await sender.Send(command, cancellationToken);
        WriteRefreshTokenCookie(tokens);
        return Ok(new AccessTokenDto(tokens.AccessToken));
    }

    // JavaScript does not read the cookie, and only the routes of /api/v1/auth receive it (TechSpec §9, ADR-0007).
    private void WriteRefreshTokenCookie(SessionTokensDto tokens)
    {
        Response.Cookies.Append(RefreshTokenCookie, tokens.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = configuration.GetValue("RefreshCookie:Secure", defaultValue: true),
            SameSite = SameSiteMode.Strict,
            Path = RefreshTokenCookiePath,
            Expires = tokens.RefreshTokenExpiresAt,
        });
    }
}

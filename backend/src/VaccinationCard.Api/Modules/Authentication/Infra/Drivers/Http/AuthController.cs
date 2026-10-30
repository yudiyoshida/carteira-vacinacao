using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using VaccinationCard.Api.Core.Messages;
using VaccinationCard.Api.Modules.Authentication.Application.Dtos;
using VaccinationCard.Api.Modules.Authentication.Application.UseCases.RefreshSession;
using VaccinationCard.Api.Modules.Authentication.Application.UseCases.SignIn;
using VaccinationCard.Api.Modules.Authentication.Application.UseCases.SignOut;
using VaccinationCard.Api.Shared.Errors;

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

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EndpointSummary("Renova a sessão do operador")]
    [ProducesResponseType<AccessTokenDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<AccessTokenDto>> Refresh(CancellationToken cancellationToken)
    {
        try
        {
            // Without the cookie, the empty token fails the validation like any other invalid token.
            var tokens = await sender.Send(new RefreshSessionCommand(Request.Cookies[RefreshTokenCookie] ?? string.Empty), cancellationToken);
            WriteRefreshTokenCookie(tokens);
            return Ok(new AccessTokenDto(tokens.AccessToken));
        }
        catch (UnauthorizedException)
        {
            // The exception handler clears the headers before writing the error, so the deletion waits for the response
            // to start. An unexpected failure keeps the cookie, because the session may still be valid (TechSpec §9).
            Response.OnStarting(() =>
            {
                DeleteRefreshTokenCookie();
                return Task.CompletedTask;
            });
            throw;
        }
    }

    [HttpPost("logout")]
    [EndpointSummary("Encerra a sessão do operador")]
    [ProducesResponseType<SuccessMessage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<SuccessMessage>> Logout(CancellationToken cancellationToken)
    {
        // MapInboundClaims is off (AuthenticationModule), so the claim keeps the name that JwtTokenService writes.
        var accountId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

        var output = await sender.Send(new SignOutCommand(accountId), cancellationToken);
        DeleteRefreshTokenCookie();
        return Ok(output);
    }

    private void WriteRefreshTokenCookie(SessionTokensDto tokens)
    {
        var options = CreateRefreshTokenCookieOptions();
        options.Expires = tokens.RefreshTokenExpiresAt;
        Response.Cookies.Append(RefreshTokenCookie, tokens.RefreshToken, options);
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookie, CreateRefreshTokenCookieOptions());
    }

    // JavaScript does not read the cookie, and only the routes of /api/v1/auth receive it (TechSpec §9, ADR-0007). The
    // deletion uses the same options: a browser matches the cookie by its path and ignores a Secure one from plain http.
    private CookieOptions CreateRefreshTokenCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = configuration.GetValue("RefreshCookie:Secure", defaultValue: true),
            SameSite = SameSiteMode.Strict,
            Path = RefreshTokenCookiePath,
        };
    }
}

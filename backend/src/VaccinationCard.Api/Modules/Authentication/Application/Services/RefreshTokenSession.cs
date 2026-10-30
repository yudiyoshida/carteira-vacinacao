using System.Security.Cryptography;
using System.Text;
using VaccinationCard.Api.Modules.Authentication.Application.Dtos;
using VaccinationCard.Api.Modules.Authentication.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Authentication.Application.Security;
using VaccinationCard.Api.Modules.Authentication.Domain.Errors;

namespace VaccinationCard.Api.Modules.Authentication.Application.Services;

// The server keeps only the hash of the refresh token, in one session per account (RN23, ADR-0007).
public sealed class RefreshTokenSession(ITokenService tokenService, ISessionDao dao)
{
    // The new hash replaces the one that the account had: a new login ends the previous session, and a renewal
    // invalidates the previous refresh token (RN23).
    public async Task<SessionTokensDto> IssueAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var accessToken = tokenService.IssueAccessToken(accountId);
        var refreshToken = tokenService.IssueRefreshToken(accountId);
        await dao.SaveAsync(new SessionDto(accountId, Hash(refreshToken.Token), refreshToken.ExpiresAt), cancellationToken);

        return new SessionTokensDto(accessToken, refreshToken.Token, refreshToken.ExpiresAt);
    }

    public async Task<Guid> ValidateAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var accountId = await tokenService.ValidateRefreshTokenAsync(refreshToken) ?? throw AuthenticationErrors.InvalidRefreshToken();

        var session = await dao.FindByAccountIdAsync(accountId, cancellationToken);
        return session?.RefreshTokenHash == Hash(refreshToken) ? accountId : throw AuthenticationErrors.InvalidRefreshToken();
    }

    public Task RevokeAsync(Guid accountId, CancellationToken cancellationToken)
    {
        return dao.DeleteAsync(accountId, cancellationToken);
    }

    private static string Hash(string refreshToken)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }
}

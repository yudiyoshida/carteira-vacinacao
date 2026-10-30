using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using VaccinationCard.Api.Modules.Authentication.Application.Dtos;
using VaccinationCard.Api.Modules.Authentication.Application.Security;

namespace VaccinationCard.Api.Modules.Authentication.Infra.Driven.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    private readonly JsonWebTokenHandler _handler = new();
    private readonly JwtOptions _jwt = options.Value;

    public string IssueAccessToken(Guid accountId)
    {
        var now = timeProvider.GetUtcNow();
        var claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Sub] = accountId.ToString() };

        return Issue(claims, _jwt.AccessTokenSecret, now, now.AddMinutes(_jwt.AccessTokenMinutes));
    }

    public RefreshTokenDto IssueRefreshToken(Guid accountId)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddDays(_jwt.RefreshTokenDays);

        // The jti tells apart two tokens of the same account and the same second, so a rotation always changes the hash.
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = accountId.ToString(),
            [JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString(),
        };

        return new RefreshTokenDto(Issue(claims, _jwt.RefreshTokenSecret, now, expiresAt), expiresAt);
    }

    public async Task<Guid?> ValidateRefreshTokenAsync(string refreshToken)
    {
        var result = await _handler.ValidateTokenAsync(refreshToken, _jwt.CreateValidationParameters(_jwt.RefreshTokenSecret, timeProvider));

        return result.IsValid && Guid.TryParse(result.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var accountId)
            ? accountId
            : null;
    }

    private string Issue(Dictionary<string, object> claims, string secret, DateTimeOffset now, DateTimeOffset expiresAt)
    {
        return _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Claims = claims,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(JwtOptions.CreateSigningKey(secret), SecurityAlgorithms.HmacSha256),
        });
    }
}

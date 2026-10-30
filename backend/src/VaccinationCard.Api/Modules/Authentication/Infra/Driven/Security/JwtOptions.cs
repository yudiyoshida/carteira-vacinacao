using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace VaccinationCard.Api.Modules.Authentication.Infra.Driven.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    private const int MinSecretBytes = 32;

    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string AccessTokenSecret { get; set; } = string.Empty;

    public string RefreshTokenSecret { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; }

    public int RefreshTokenDays { get; set; }

    public static SymmetricSecurityKey CreateSigningKey(string secret)
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }

    // Each kind of token has a secret of its own, so one never passes for the other (TechSpec §9, ADR-0007).
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Issuer)
            && !string.IsNullOrWhiteSpace(Audience)
            && Encoding.UTF8.GetByteCount(AccessTokenSecret) >= MinSecretBytes
            && Encoding.UTF8.GetByteCount(RefreshTokenSecret) >= MinSecretBytes
            && AccessTokenSecret != RefreshTokenSecret
            && AccessTokenMinutes > 0
            && RefreshTokenDays > 0;
    }

    // The library reads the real clock. The lifetime check reads the TimeProvider of the API, the same clock that issues
    // the tokens (TechSpec §9).
    public TokenValidationParameters CreateValidationParameters(string secret, TimeProvider timeProvider)
    {
        return new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            IssuerSigningKey = CreateSigningKey(secret),
            ClockSkew = ClockSkew,
            LifetimeValidator = (notBefore, expires, _, parameters) =>
            {
                var now = timeProvider.GetUtcNow().UtcDateTime;
                return now <= expires + parameters.ClockSkew
                    && (notBefore is null || now >= notBefore - parameters.ClockSkew);
            },
        };
    }
}

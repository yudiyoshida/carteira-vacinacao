using VaccinationCard.Api.Modules.Authentication.Application.Dtos;

namespace VaccinationCard.Api.Modules.Authentication.Application.Security;

public interface ITokenService
{
    string IssueAccessToken(Guid accountId);

    RefreshTokenDto IssueRefreshToken(Guid accountId);

    // The account of a token with a valid signature and within its validity; null for any other token.
    Task<Guid?> ValidateRefreshTokenAsync(string refreshToken);
}

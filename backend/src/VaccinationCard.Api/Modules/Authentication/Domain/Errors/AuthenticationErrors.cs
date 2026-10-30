using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.Api.Modules.Authentication.Domain.Errors;

public static class AuthenticationErrors
{
    public static UnauthorizedException InvalidCredentials()
    {
        return new("auth.invalid_credentials", "E-mail ou senha inválidos.");
    }

    public static UnauthorizedException InvalidRefreshToken()
    {
        return new("auth.invalid_refresh_token", "Sessão expirada. Entre novamente.");
    }
}

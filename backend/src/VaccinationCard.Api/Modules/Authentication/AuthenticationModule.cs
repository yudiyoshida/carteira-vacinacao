using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using VaccinationCard.Api.Infra.Http;
using VaccinationCard.Api.Modules.Authentication.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Authentication.Application.Security;
using VaccinationCard.Api.Modules.Authentication.Application.Services;
using VaccinationCard.Api.Modules.Authentication.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Modules.Authentication.Infra.Driven.Security;

namespace VaccinationCard.Api.Modules.Authentication;

public static class AuthenticationModule
{
    public static IServiceCollection AddAuthenticationModule(this IServiceCollection services)
    {
        services.AddScoped<ISessionDao, SessionDao>();
        services.AddScoped<RefreshTokenSession>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate(options => options.IsValid(), "A seção 'Jwt' da configuração é inválida: os segredos precisam de pelo menos 32 bytes e devem ser diferentes entre si; issuer e audience são obrigatórios; as validades devem ser positivas.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, TimeProvider>((bearer, options, timeProvider) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = options.Value.CreateValidationParameters(options.Value.AccessTokenSecret, timeProvider);
                bearer.Events = new JwtBearerEvents { OnChallenge = WriteUnauthenticatedAsync };
            });

        // Every route requires the access token, unless it declares anonymous access (RN21).
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }

    // Handling the response here also skips the WWW-Authenticate header of the JwtBearer handler, so it goes out here too.
    private static async Task WriteUnauthenticatedAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        context.Response.Headers.WWWAuthenticate = JwtBearerDefaults.AuthenticationScheme;

        var problem = ApiProblemDetails.Create(context.HttpContext, StatusCodes.Status401Unauthorized, "auth.unauthenticated", "Autenticação necessária.");
        await ApiProblemDetails.WriteAsync(context.HttpContext, problem, context.HttpContext.RequestAborted);
    }
}

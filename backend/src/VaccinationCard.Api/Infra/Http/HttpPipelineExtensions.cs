using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;

namespace VaccinationCard.Api.Infra.Http;

public static class HttpPipelineExtensions
{
    public static IServiceCollection AddHttpPipeline(this IServiceCollection services)
    {
        services
            // The validators are the only source of field messages, so MVC does not require the non-nullable fields.
            .AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
            .AddJsonOptions(options => UseEnumNames(options.JsonSerializerOptions))
            .ConfigureApiBehaviorOptions(options =>
            {
                // MVC client errors without a body (415) go to the status code pages, which add the code.
                options.SuppressMapClientErrors = true;
                options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create;
            });

        // The OpenAPI document describes the bodies with the HTTP JSON options, not with the MVC ones.
        services.ConfigureHttpJsonOptions(options => UseEnumNames(options.SerializerOptions));

        // UseExceptionHandler() without a path or delegate requires the ProblemDetails services.
        services.AddProblemDetails();
        services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

        services.AddOptions<ForwardedHeadersOptions>().Configure<IConfiguration>(TrustOnlyKnownProxies);
        services.AddRateLimiter(LoginRateLimit.Configure);

        return services;
    }

    // The order matters: the forwarded headers restore the client IP that the rate limit counts, the security headers
    // wrap every response, the request log sees the status that the error handlers set, and the error handlers also
    // cover the answers of the access control. A refused login attempt does not reach the validation of the token.
    public static WebApplication UseHttpPipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseExceptionHandler();
        app.UseStatusCodePages(StatusCodeProblemDetails.WriteAsync);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    // The default lists trust the loopback addresses, and two empty lists would trust every client, so without a proxy
    // the middleware reads no header at all (TechSpec §9).
    private static void TrustOnlyKnownProxies(ForwardedHeadersOptions options, IConfiguration configuration)
    {
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();

        var proxies = (configuration["ForwardedHeaders:KnownProxies"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var proxy in proxies)
        {
            options.KnownProxies.Add(IPAddress.Parse(proxy));
        }

        options.ForwardedHeaders = proxies.Length > 0 ? ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto : ForwardedHeaders.None;
    }

    // Enums go by name, and a number in place of the name is refused (TechSpec §7.1).
    private static void UseEnumNames(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    }
}

using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace VaccinationCard.Api.Infra.Http;

// The limit of login attempts by IP (RN24, TechSpec §9).
public static class LoginRateLimit
{
    public const string PolicyName = "login";

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = WriteRejectionAsync;
        options.AddPolicy(PolicyName, httpContext => RateLimitPartition.GetFixedWindowLimiter(
            ClientIp(httpContext),
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
    }

    // Kestrel reports a direct IPv4 client mapped to IPv6, and the proxy forwards the plain IPv4, so both count as one IP.
    // Requests without an IP share one limit instead of having none.
    private static IPAddress ClientIp(HttpContext httpContext)
    {
        var ip = httpContext.Connection.RemoteIpAddress ?? IPAddress.None;
        return ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip;
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var problem = ApiProblemDetails.Create(context.HttpContext, StatusCodes.Status429TooManyRequests, "auth.too_many_attempts", "Muitas tentativas de login. Aguarde um minuto e tente de novo.");
        await ApiProblemDetails.WriteAsync(context.HttpContext, problem, cancellationToken);
    }
}

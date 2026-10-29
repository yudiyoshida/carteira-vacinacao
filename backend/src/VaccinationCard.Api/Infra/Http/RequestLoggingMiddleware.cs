using System.Diagnostics;

namespace VaccinationCard.Api.Infra.Http;

public sealed partial class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var startedAt = Stopwatch.GetTimestamp();

        await next(context);

        // The path goes without the query string, because a search term can be a person's name (RN25).
        LogRequest(logger, context.Request.Method, context.Request.Path.Value, context.Response.StatusCode, Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "HTTP {Method} {Path} respondeu {StatusCode} em {ElapsedMilliseconds:0.0} ms")]
    private static partial void LogRequest(ILogger logger, string method, string? path, int statusCode, double elapsedMilliseconds);
}

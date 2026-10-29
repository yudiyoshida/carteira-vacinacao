namespace VaccinationCard.Api.Infra.Http;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        // Written when the response starts, because the exception handler clears the headers before writing an error.
        context.Response.OnStarting(static state =>
        {
            var headers = ((HttpResponse)state).Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            return Task.CompletedTask;
        }, context.Response);

        return next(context);
    }
}

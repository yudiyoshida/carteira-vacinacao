namespace VaccinationCard.Api.Infra.Http;

public static class HttpPipelineExtensions
{
    public static IServiceCollection AddHttpPipeline(this IServiceCollection services)
    {
        services
            // The validators are the only source of field messages, so MVC does not require the non-nullable fields.
            .AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
            .ConfigureApiBehaviorOptions(options =>
            {
                // MVC client errors without a body (415) go to the status code pages, which add the code.
                options.SuppressMapClientErrors = true;
                options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create;
            });

        // UseExceptionHandler() without a path or delegate requires the ProblemDetails services.
        services.AddProblemDetails();
        services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

        return services;
    }

    // The order matters: the security headers wrap every response, and the request log sees the status that the error
    // handlers set.
    public static WebApplication UseHttpPipeline(this WebApplication app)
    {
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseExceptionHandler();
        app.UseStatusCodePages(StatusCodeProblemDetails.WriteAsync);

        return app;
    }
}

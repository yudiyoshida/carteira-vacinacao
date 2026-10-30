using System.Text.Json;
using System.Text.Json.Serialization;

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

        return services;
    }

    // The order matters: the security headers wrap every response, the request log sees the status that the error
    // handlers set, and the error handlers also cover the answers of the access control.
    public static WebApplication UseHttpPipeline(this WebApplication app)
    {
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseExceptionHandler();
        app.UseStatusCodePages(StatusCodeProblemDetails.WriteAsync);
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    // Enums go by name, and a number in place of the name is refused (TechSpec §7.1).
    private static void UseEnumNames(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    }
}

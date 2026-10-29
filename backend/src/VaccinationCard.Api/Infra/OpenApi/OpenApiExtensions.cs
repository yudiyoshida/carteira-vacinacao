using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace VaccinationCard.Api.Infra.OpenApi;

public static class OpenApiExtensions
{
    private const string Title = "Cartão de Vacinação";

    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        return services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo { Title = Title, Version = "v1" };
            return Task.CompletedTask;
        }));
    }

    public static WebApplication MapOpenApiDocumentation(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("OpenApi:Enabled"))
        {
            return app;
        }

        app.MapOpenApi();
        app.MapScalarApiReference(options => options.WithTitle(Title));

        return app;
    }
}

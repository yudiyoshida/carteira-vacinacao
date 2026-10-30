using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace VaccinationCard.Api.Infra.OpenApi;

public static class OpenApiExtensions
{
    private const string Title = "Cartão de Vacinação";
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        return services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo { Title = Title, Version = "v1" };
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    [BearerScheme] = new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" },
                };
                return Task.CompletedTask;
            });

            // Every route requires the access token, except the ones that declare anonymous access (TechSpec §7.1).
            options.AddOperationTransformer((operation, context, _) =>
            {
                if (!context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
                {
                    operation.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(BearerScheme, context.Document)] = [] }];
                }

                return Task.CompletedTask;
            });
        });
    }

    public static WebApplication MapOpenApiDocumentation(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("OpenApi:Enabled"))
        {
            return app;
        }

        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference(options => options.WithTitle(Title).AddPreferredSecuritySchemes(BearerScheme)).AllowAnonymous();

        return app;
    }
}

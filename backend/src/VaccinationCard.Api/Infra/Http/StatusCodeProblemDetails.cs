using Microsoft.AspNetCore.Diagnostics;

namespace VaccinationCard.Api.Infra.Http;

// Gives a body to the errors that routing and MVC answer without one (TechSpec §8.3).
public static class StatusCodeProblemDetails
{
    private static readonly Dictionary<int, (string Code, string Detail)> Errors = new()
    {
        [StatusCodes.Status404NotFound] = ("route.not_found", "Rota não encontrada."),
        [StatusCodes.Status405MethodNotAllowed] = ("route.method_not_allowed", "Método não permitido nesta rota."),
        [StatusCodes.Status415UnsupportedMediaType] = ("request.unsupported_media_type", "Tipo de conteúdo não suportado. Envie JSON."),
    };

    public static Task WriteAsync(StatusCodeContext context)
    {
        var httpContext = context.HttpContext;
        var status = httpContext.Response.StatusCode;
        if (!Errors.TryGetValue(status, out var error))
        {
            return Task.CompletedTask;
        }

        var problem = ApiProblemDetails.Create(httpContext, status, error.Code, error.Detail);
        return ApiProblemDetails.WriteAsync(httpContext, problem, httpContext.RequestAborted);
    }
}

using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace VaccinationCard.Api.Infra.Http;

// Builds every error response of the API in the format of TechSpec §8.1.
public static class ApiProblemDetails
{
    // Titles by status (TechSpec §8.2).
    private static readonly Dictionary<int, string> Titles = new()
    {
        [StatusCodes.Status400BadRequest] = "Requisição inválida",
        [StatusCodes.Status401Unauthorized] = "Não autorizado",
        [StatusCodes.Status404NotFound] = "Recurso não encontrado",
        [StatusCodes.Status405MethodNotAllowed] = "Método não permitido",
        [StatusCodes.Status409Conflict] = "Conflito",
        [StatusCodes.Status415UnsupportedMediaType] = "Tipo de conteúdo não suportado",
        [StatusCodes.Status422UnprocessableEntity] = "Regra de negócio violada",
        [StatusCodes.Status429TooManyRequests] = "Muitas requisições",
        [StatusCodes.Status500InternalServerError] = "Erro inesperado",
    };

    public static ProblemDetails Create(HttpContext httpContext, int status, string code, string detail) =>
        Fill(new ProblemDetails(), httpContext, status, code, detail);

    public static HttpValidationProblemDetails CreateValidation(HttpContext httpContext, IEnumerable<(string Field, string Message)> errors)
    {
        var errorsByField = errors
            .GroupBy(error => ToJsonName(error.Field), error => error.Message)
            .ToDictionary(group => group.Key, group => group.Distinct().ToArray());

        return Fill(new HttpValidationProblemDetails(errorsByField), httpContext, StatusCodes.Status400BadRequest, "validation.failed", "Um ou mais campos são inválidos.");
    }

    // Ignores the Accept header, as MVC does: the default IProblemDetailsService writes nothing when the client
    // accepts only text/html, and every error must still go out in this format.
    public static Task WriteAsync(HttpContext httpContext, ProblemDetails problem, CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = problem.Status!.Value;
        return httpContext.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, "application/problem+json", cancellationToken);
    }

    public static string TraceId(HttpContext httpContext) => Activity.Current?.Id ?? httpContext.TraceIdentifier;

    private static TProblem Fill<TProblem>(TProblem problem, HttpContext httpContext, int status, string code, string detail)
        where TProblem : ProblemDetails
    {
        problem.Status = status;
        problem.Title = Titles[status];
        problem.Detail = detail;
        problem.Instance = httpContext.Request.Path.Value;
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = TraceId(httpContext);
        return problem;
    }

    // "Dose.Number" becomes "dose.number": the keys of "errors" are the JSON fields.
    private static string ToJsonName(string field) =>
        string.Join('.', field.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}

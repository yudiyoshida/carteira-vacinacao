using Microsoft.AspNetCore.Mvc;

namespace VaccinationCard.Api.Infra.Http;

// MVC answers before the use case when it cannot read the body or a parameter: malformed JSON, a value of the wrong
// type or an unknown enum name. The answer keeps the validation format, keyed by the JSON field.
public static class InvalidModelStateResponse
{
    public static IActionResult Create(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .Select(entry => FieldError(entry.Key));

        return new ObjectResult(ApiProblemDetails.CreateValidation(context.HttpContext, errors))
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" },
        };
    }

    // System.Text.Json names a field by its JSON path ("$.dose.type"); the bare root, or no key, is the whole body.
    private static (string Field, string Message) FieldError(string key)
    {
        var field = key.StartsWith('$') ? key.TrimStart('$').TrimStart('.') : key;
        return field.Length == 0
            ? ("body", "O corpo da requisição é inválido.")
            : (field, "O valor informado é inválido.");
    }
}

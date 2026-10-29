using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.Api.Infra.Http;

public sealed partial class ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    private const string UnexpectedCode = "internal.unexpected";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem = exception switch
        {
            ValidationException validation => ApiProblemDetails.CreateValidation(httpContext, validation.Errors.Select(failure => (failure.PropertyName, failure.ErrorMessage))),
            UnauthorizedException unauthorized => ApiProblemDetails.Create(httpContext, StatusCodes.Status401Unauthorized, unauthorized.Code, unauthorized.Message),
            NotFoundException notFound => ApiProblemDetails.Create(httpContext, StatusCodes.Status404NotFound, notFound.Code, notFound.Message),
            ConflictException conflict => ApiProblemDetails.Create(httpContext, StatusCodes.Status409Conflict, conflict.Code, conflict.Message),
            BusinessRuleException businessRule => ApiProblemDetails.Create(httpContext, StatusCodes.Status422UnprocessableEntity, businessRule.Code, businessRule.Message),
            _ => Unexpected(httpContext, exception),
        };

        await ApiProblemDetails.WriteAsync(httpContext, problem, cancellationToken);
        return true;
    }

    // The details go only to the log, and the request body goes to neither (TechSpec §8.2, RN25).
    private ProblemDetails Unexpected(HttpContext httpContext, Exception exception)
    {
        LogUnexpectedException(logger, exception, exception.GetType().FullName, UnexpectedCode, ApiProblemDetails.TraceId(httpContext));
        return ApiProblemDetails.Create(httpContext, StatusCodes.Status500InternalServerError, UnexpectedCode, "Ocorreu um erro inesperado.");
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Exceção inesperada {ExceptionType}, respondida com {Code} (trace {TraceId})")]
    private static partial void LogUnexpectedException(ILogger logger, Exception exception, string? exceptionType, string code, string traceId);
}

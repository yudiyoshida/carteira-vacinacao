using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using VaccinationCard.Api.Infra.Http;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.UnitTests.Infra.Http;

public sealed class ProblemDetailsExceptionHandlerTests
{
    private const string RequestPath = "/api/v1/people/0192f1a0-7c1e-7000-8000-000000000001/vaccinations";

    private readonly FakeLogger<ProblemDetailsExceptionHandler> _logger = new();
    private readonly ProblemDetailsExceptionHandler _sut;

    public ProblemDetailsExceptionHandlerTests() => _sut = new ProblemDetailsExceptionHandler(_logger);

    public static TheoryData<Exception, int, string, string> CategoryExceptions => new()
    {
        { new UnauthorizedException("auth.invalid_credentials", "E-mail ou senha inválidos."), 401, "Não autorizado", "auth.invalid_credentials" },
        { new NotFoundException("person.not_found", "Pessoa não encontrada."), 404, "Recurso não encontrado", "person.not_found" },
        { new ConflictException("person.cpf_already_registered", "Já existe uma pessoa cadastrada com este CPF."), 409, "Conflito", "person.cpf_already_registered" },
        { new BusinessRuleException("vaccination.dose_out_of_sequence", "A dose informada não é a próxima dose esperada desta vacina."), 422, "Regra de negócio violada", "vaccination.dose_out_of_sequence" },
    };

    [Theory]
    [MemberData(nameof(CategoryExceptions))]
    public async Task Should_Answer_With_The_Category_Status_When_Exception_Has_A_Category(Exception exception, int status, string title, string code)
    {
        // Arrange
        using var activity = new Activity("request").Start();
        var httpContext = MakeHttpContext();

        // Act
        var handled = await _sut.TryHandleAsync(httpContext, exception, TestContext.Current.CancellationToken);

        // Assert
        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(status);
        httpContext.Response.ContentType.ShouldBe("application/problem+json");
        var body = ParseBody(httpContext);
        body.GetProperty("status").GetInt32().ShouldBe(status);
        body.GetProperty("title").GetString().ShouldBe(title);
        body.GetProperty("detail").GetString().ShouldBe(exception.Message);
        body.GetProperty("instance").GetString().ShouldBe(RequestPath);
        body.GetProperty("code").GetString().ShouldBe(code);
        body.GetProperty("traceId").GetString().ShouldBe(activity.Id);
    }

    [Fact]
    public async Task Should_Answer_Bad_Request_With_Errors_By_Json_Field_When_Validation_Fails()
    {
        // Arrange
        var httpContext = MakeHttpContext();
        var exception = new ValidationException(
        [
            new ValidationFailure("Name", "O nome é obrigatório."),
            new ValidationFailure("Dose.Number", "O número da dose deve ser maior que zero."),
            new ValidationFailure("Name", "O nome deve ter entre 3 e 150 caracteres."),
        ]);

        // Act
        await _sut.TryHandleAsync(httpContext, exception, TestContext.Current.CancellationToken);

        // Assert
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        var body = ParseBody(httpContext);
        body.GetProperty("title").GetString().ShouldBe("Requisição inválida");
        body.GetProperty("detail").GetString().ShouldBe("Um ou mais campos são inválidos.");
        body.GetProperty("code").GetString().ShouldBe("validation.failed");
        var errors = body.GetProperty("errors");
        errors.EnumerateObject().Select(field => field.Name).ShouldBe(["name", "dose.number"]);
        Messages(errors, "name").ShouldBe(["O nome é obrigatório.", "O nome deve ter entre 3 e 150 caracteres."]);
        Messages(errors, "dose.number").ShouldBe(["O número da dose deve ser maior que zero."]);
    }

    [Fact]
    [Trait("RN", "RN25")]
    public async Task Should_Hide_Exception_Details_When_Exception_Is_Unexpected()
    {
        // Arrange
        var httpContext = MakeHttpContext();
        var exception = new InvalidOperationException("Falha ao gravar o CPF 123.456.789-09.");

        // Act
        await _sut.TryHandleAsync(httpContext, exception, TestContext.Current.CancellationToken);

        // Assert
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        ReadBody(httpContext).ShouldNotContain("123.456.789-09");
        var body = ParseBody(httpContext);
        body.GetProperty("title").GetString().ShouldBe("Erro inesperado");
        body.GetProperty("detail").GetString().ShouldBe("Ocorreu um erro inesperado.");
        body.GetProperty("code").GetString().ShouldBe("internal.unexpected");
    }

    [Fact]
    public async Task Should_Log_Type_Code_And_Trace_Id_When_Exception_Is_Unexpected()
    {
        // Arrange
        using var activity = new Activity("request").Start();
        var exception = new InvalidOperationException("Falha interna.");

        // Act
        await _sut.TryHandleAsync(MakeHttpContext(), exception, TestContext.Current.CancellationToken);

        // Assert
        var record = _logger.LatestRecord;
        record.Level.ShouldBe(LogLevel.Error);
        record.Exception.ShouldBeSameAs(exception);
        record.GetStructuredStateValue("ExceptionType").ShouldBe(typeof(InvalidOperationException).FullName);
        record.GetStructuredStateValue("Code").ShouldBe("internal.unexpected");
        record.GetStructuredStateValue("TraceId").ShouldBe(activity.Id);
    }

    [Fact]
    public async Task Should_Answer_Problem_Details_When_Accept_Header_Excludes_Json()
    {
        // Arrange
        var httpContext = MakeHttpContext();
        httpContext.Request.Headers.Accept = "text/html";
        var exception = new NotFoundException("person.not_found", "Pessoa não encontrada.");

        // Act
        await _sut.TryHandleAsync(httpContext, exception, TestContext.Current.CancellationToken);

        // Assert
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        httpContext.Response.ContentType.ShouldBe("application/problem+json");
        ParseBody(httpContext).GetProperty("code").GetString().ShouldBe("person.not_found");
    }

    [Fact]
    public async Task Should_Not_Log_When_Exception_Has_A_Category()
    {
        // Arrange
        var exception = new NotFoundException("person.not_found", "Pessoa não encontrada.");

        // Act
        await _sut.TryHandleAsync(MakeHttpContext(), exception, TestContext.Current.CancellationToken);

        // Assert
        _logger.Collector.Count.ShouldBe(0);
    }

    private static DefaultHttpContext MakeHttpContext() => new()
    {
        Request = { Path = RequestPath },
        Response = { Body = new MemoryStream() },
    };

    private static string ReadBody(HttpContext httpContext) =>
        Encoding.UTF8.GetString(((MemoryStream)httpContext.Response.Body).ToArray());

    private static JsonElement ParseBody(HttpContext httpContext) =>
        JsonDocument.Parse(ReadBody(httpContext)).RootElement;

    private static string?[] Messages(JsonElement errors, string field) =>
        [.. errors.GetProperty(field).EnumerateArray().Select(message => message.GetString())];
}

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace VaccinationCard.IntegrationTests.Infra.Http;

public sealed class ErrorResponseTests(IntegrationFixture fixture) : PipelineTest(fixture)
{
    [Fact]
    [Trait("RN", "RN25")]
    public async Task Should_Hide_Exception_Details_When_Exception_Is_Unexpected()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.GetAsync("/test/unexpected", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("internal.unexpected");
        problem.GetProperty("detail").GetString().ShouldBe("Ocorreu um erro inesperado.");
        problem.GetRawText().ShouldNotContain(PipelineTestController.Cpf);
    }

    [Fact]
    public async Task Should_Answer_Not_Found_Problem_When_Route_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.GetAsync("/api/v1/does-not-exist", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("route.not_found");
        problem.GetProperty("instance").GetString().ShouldBe("/api/v1/does-not-exist");
    }

    [Fact]
    public async Task Should_Answer_Method_Not_Allowed_Problem_When_Route_Does_Not_Accept_The_Method()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.DeleteAsync("/test/unexpected", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("route.method_not_allowed");
    }

    [Fact]
    public async Task Should_Answer_Unsupported_Media_Type_Problem_When_Body_Is_Not_Json()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var body = new StringContent("name=Maria", Encoding.UTF8, "text/plain");

        // Act
        var response = await Client.PostAsync("/test/echo", body, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("request.unsupported_media_type");
    }

    [Fact]
    public async Task Should_Answer_Validation_Problem_When_Json_Is_Malformed()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var body = new StringContent("{", Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/test/echo", body, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        Fields(problem).ShouldBe(["body"]);
    }

    [Fact]
    public async Task Should_Answer_Validation_Problem_With_The_Field_When_A_Value_Has_The_Wrong_Type()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var body = new StringContent("""{ "name": "Maria", "number": "três" }""", Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/test/echo", body, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        Fields(problem).ShouldBe(["number"]);
    }

    [Fact]
    public async Task Should_Leave_Required_Fields_To_The_Validators_When_A_Field_Is_Missing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.PostAsJsonAsync("/test/echo", new { number = 1 }, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_Answer_Validation_Problem_With_Json_Fields_When_A_Validator_Fails()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.PostAsJsonAsync("/test/validated", new { name = "" }, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        Fields(problem).ShouldBe(["name"]);
        problem.GetProperty("errors").GetProperty("name")[0].GetString().ShouldBe("O nome é obrigatório.");
    }

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    private static string[] Fields(JsonElement problem) =>
        [.. problem.GetProperty("errors").EnumerateObject().Select(field => field.Name)];
}

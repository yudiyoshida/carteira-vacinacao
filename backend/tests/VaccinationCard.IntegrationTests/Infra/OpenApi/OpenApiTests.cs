using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace VaccinationCard.IntegrationTests.Infra.OpenApi;

public sealed class OpenApiTests(IntegrationFixture fixture)
{
    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar")]
    public async Task Should_Serve_Documentation_When_OpenApi_Is_Enabled(string path)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = fixture.Factory.WithWebHostBuilder(builder => builder.UseSetting("OpenApi:Enabled", "true"));
        using var client = fixture.Factory.CreateClient();

        // Act
        var response = await client.GetAsync(path, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // The bodies carry enums by name (TechSpec §7.1), and the document must describe them the same way.
    [Fact]
    public async Task Should_Describe_Enums_By_Name_When_Serving_The_Document()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.Factory.CreateClient();

        // Act
        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json", cancellationToken);

        // Assert
        var doseType = document.GetProperty("components").GetProperty("schemas").GetProperty("DoseType");
        doseType.GetProperty("enum").EnumerateArray().Select(value => value.GetString()).ShouldBe(["Primary", "Booster"]);
    }

    // Scalar reads the scheme from the document to send the access token (TechSpec §7.1).
    [Fact]
    public async Task Should_Declare_The_Bearer_Scheme_When_Serving_The_Document()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.Factory.CreateClient();

        // Act
        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json", cancellationToken);

        // Assert
        var scheme = document.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        scheme.GetProperty("type").GetString().ShouldBe("http");
        scheme.GetProperty("scheme").GetString().ShouldBe("bearer");
    }

    [Fact]
    [Trait("RN", "RN21")]
    public async Task Should_Require_The_Bearer_Scheme_Only_On_Protected_Routes_When_Serving_The_Document()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.Factory.CreateClient();

        // Act
        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json", cancellationToken);

        // Assert
        var paths = document.GetProperty("paths");
        var requirement = paths.GetProperty("/api/v1/vaccines").GetProperty("get").GetProperty("security").EnumerateArray().Single();
        requirement.EnumerateObject().Select(scheme => scheme.Name).ShouldBe(["Bearer"]);
        paths.GetProperty("/api/v1/auth/login").GetProperty("post").TryGetProperty("security", out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar")]
    public async Task Should_Return_Not_Found_When_OpenApi_Is_Disabled(string path)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = fixture.Factory.WithWebHostBuilder(builder => builder.UseSetting("OpenApi:Enabled", "false"));
        using var client = factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync(path, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

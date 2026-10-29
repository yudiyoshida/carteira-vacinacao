using System.Net;
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

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar")]
    public async Task Should_Return_Not_Found_When_OpenApi_Is_Disabled(string path)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = fixture.Factory.WithWebHostBuilder(builder => builder.UseSetting("OpenApi:Enabled", "false"));
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(path, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

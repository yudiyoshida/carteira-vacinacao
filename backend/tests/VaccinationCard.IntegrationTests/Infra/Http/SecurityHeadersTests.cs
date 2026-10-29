using System.Net;

namespace VaccinationCard.IntegrationTests.Infra.Http;

public sealed class SecurityHeadersTests(IntegrationFixture fixture) : PipelineTest(fixture)
{
    [Theory]
    [InlineData("/health", HttpStatusCode.OK)]
    [InlineData("/test/unexpected", HttpStatusCode.InternalServerError)]
    [InlineData("/api/v1/does-not-exist", HttpStatusCode.NotFound)]
    public async Task Should_Send_Security_Headers_When_Responding(string path, HttpStatusCode status)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.GetAsync(path, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(status);
        response.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
        response.Headers.GetValues("X-Frame-Options").ShouldBe(["DENY"]);
        response.Headers.GetValues("Referrer-Policy").ShouldBe(["no-referrer"]);
    }
}

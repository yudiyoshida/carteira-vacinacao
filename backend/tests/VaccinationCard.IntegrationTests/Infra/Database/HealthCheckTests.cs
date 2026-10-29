using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace VaccinationCard.IntegrationTests.Infra.Database;

public sealed class HealthCheckTests(IntegrationFixture fixture)
{
    private const string UnreachableDatabase = "Host=127.0.0.1;Port=1;Database=vaccination_card;Username=postgres;Password=postgres;Timeout=1";

    [Fact]
    public async Task Should_Return_Ok_When_Database_Is_Available()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.Factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_Return_Service_Unavailable_When_Database_Is_Unavailable()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", UnreachableDatabase);
            builder.UseSetting("Database:MigrateOnStartup", "false");
        });
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }
}

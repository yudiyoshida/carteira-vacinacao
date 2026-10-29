using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using VaccinationCard.Api.Infra.Database;

namespace VaccinationCard.IntegrationTests.Infra.Database;

public sealed class MigrationTests(IntegrationFixture fixture)
{
    [Fact]
    public async Task Should_Apply_Migrations_When_Api_Starts()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Act
        var historyExists = await db.GetService<IHistoryRepository>().ExistsAsync(cancellationToken);
        var pendingMigrations = await db.Database.GetPendingMigrationsAsync(cancellationToken);

        // Assert
        historyExists.ShouldBeTrue();
        pendingMigrations.ShouldBeEmpty();
    }
}

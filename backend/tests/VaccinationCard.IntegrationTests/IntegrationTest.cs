namespace VaccinationCard.IntegrationTests;

public abstract class IntegrationTest(IntegrationFixture fixture) : IAsyncLifetime
{
    protected IntegrationFixture Fixture { get; } = fixture;

    public virtual async ValueTask InitializeAsync() =>
        await Fixture.ResetDatabaseAsync(TestContext.Current.CancellationToken);

    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

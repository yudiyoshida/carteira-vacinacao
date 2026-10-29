using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VaccinationCard.Api.Infra.Database;

namespace VaccinationCard.IntegrationTests;

public abstract class IntegrationTest(IntegrationFixture fixture) : IAsyncLifetime
{
    protected IntegrationFixture Fixture { get; } = fixture;

    public virtual async ValueTask InitializeAsync() =>
        await Fixture.ResetDatabaseAsync(TestContext.Current.CancellationToken);

    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // Each call gets a DI scope of its own, as an HTTP request does, and goes through the MediatR behaviors.
    protected async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request, cancellationToken);
    }

    // A context in a DI scope of its own: a check reads what the database stored, outside the scope that wrote.
    protected async Task<TResult> ExecuteDbContextAsync<TResult>(Func<AppDbContext, Task<TResult>> action)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}

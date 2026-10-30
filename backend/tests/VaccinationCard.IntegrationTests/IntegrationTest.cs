using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Modules.Accounts.Application.Dtos;
using VaccinationCard.Api.Modules.Accounts.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Accounts.Application.Security;

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

    // An account has no route or use case that creates it: the tests write it through the DAO, as the seed does.
    protected async Task<Guid> CreateAccountAsync(string email, string password, CancellationToken cancellationToken)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var passwordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Hash(password);
        var account = new AccountCredentialsDto(Guid.CreateVersion7(), email, passwordHash);
        await scope.ServiceProvider.GetRequiredService<IAccountDao>().SaveAsync(account, cancellationToken);
        return account.Id;
    }

    // A context in a DI scope of its own: a check reads what the database stored, outside the scope that wrote.
    protected async Task<TResult> ExecuteDbContextAsync<TResult>(Func<AppDbContext, Task<TResult>> action)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}

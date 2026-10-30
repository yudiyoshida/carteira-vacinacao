using VaccinationCard.Api.Modules.Authentication.Application.Dtos;
using VaccinationCard.Api.Modules.Authentication.Application.Persistence.Dao;

namespace VaccinationCard.IntegrationTests.Modules.Authentication.Infra.Drivers.Http;

// Stands for a database out of reach: the renewal fails without saying anything about the refresh token.
public sealed class FailingSessionDao : ISessionDao
{
    public Task SaveAsync(SessionDto session, CancellationToken cancellationToken) => throw new InvalidOperationException();

    public Task<SessionDto?> FindByAccountIdAsync(Guid accountId, CancellationToken cancellationToken) => throw new InvalidOperationException();

    public Task DeleteAsync(Guid accountId, CancellationToken cancellationToken) => throw new InvalidOperationException();
}

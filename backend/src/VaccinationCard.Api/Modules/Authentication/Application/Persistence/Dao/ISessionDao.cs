using VaccinationCard.Api.Modules.Authentication.Application.Dtos;

namespace VaccinationCard.Api.Modules.Authentication.Application.Persistence.Dao;

// Authentication has no aggregate, so its DAO also writes (ADR-0003).
public interface ISessionDao
{
    // One session per account: saving replaces the one that the account had (RN23).
    Task SaveAsync(SessionDto session, CancellationToken cancellationToken);

    Task<SessionDto?> FindByAccountIdAsync(Guid accountId, CancellationToken cancellationToken);

    Task DeleteAsync(Guid accountId, CancellationToken cancellationToken);
}

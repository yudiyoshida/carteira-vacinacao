using VaccinationCard.Api.Modules.Accounts.Application.Dtos;

namespace VaccinationCard.Api.Modules.Accounts.Application.Persistence.Dao;

// Accounts has no aggregate, so its DAO also writes (ADR-0003).
public interface IAccountDao
{
    Task<AccountCredentialsDto?> FindCredentialsByEmailAsync(string email, CancellationToken cancellationToken);

    Task<AccountDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task SaveAsync(AccountCredentialsDto account, CancellationToken cancellationToken);
}

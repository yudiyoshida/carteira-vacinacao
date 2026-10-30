using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Modules.Accounts.Application.Dtos;
using VaccinationCard.Api.Modules.Accounts.Application.Persistence.Dao;

namespace VaccinationCard.Api.Modules.Accounts.Infra.Driven.Persistence.EfCore;

public sealed class AccountDao(AppDbContext db) : IAccountDao
{
    public Task<AccountCredentialsDto?> FindCredentialsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = Normalize(email);
        return db.Set<AccountRecord>()
            .AsNoTracking()
            .Where(x => x.Email == normalizedEmail)
            .Select(x => new AccountCredentialsDto(x.Id, x.Email, x.PasswordHash))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<AccountDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return db.Set<AccountRecord>()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AccountDto(x.Id, x.Email))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(AccountCredentialsDto account, CancellationToken cancellationToken)
    {
        db.Add(new AccountRecord
        {
            Id = account.Id,
            Email = Normalize(account.Email),
            PasswordHash = account.PasswordHash,
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    // The e-mail is unique without case and without the spaces at the ends (TechSpec §5.3).
    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}

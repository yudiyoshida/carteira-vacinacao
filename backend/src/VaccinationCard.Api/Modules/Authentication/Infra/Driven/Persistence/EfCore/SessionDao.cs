using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Modules.Authentication.Application.Dtos;
using VaccinationCard.Api.Modules.Authentication.Application.Persistence.Dao;

namespace VaccinationCard.Api.Modules.Authentication.Infra.Driven.Persistence.EfCore;

public sealed class SessionDao(AppDbContext db) : ISessionDao
{
    // Two logins of the same account at the same time end in one session, without a violation of the key (RN23). The
    // change tracker has no upsert, so this one is SQL.
    public async Task SaveAsync(SessionDto session, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO sessions (account_id, refresh_token_hash, expires_at, updated_at)
            VALUES ({session.AccountId}, {session.RefreshTokenHash}, {session.ExpiresAt}, now())
            ON CONFLICT (account_id) DO UPDATE
            SET refresh_token_hash = excluded.refresh_token_hash,
                expires_at = excluded.expires_at,
                updated_at = excluded.updated_at
            """,
            cancellationToken);
    }

    public Task<SessionDto?> FindByAccountIdAsync(Guid accountId, CancellationToken cancellationToken)
    {
        return db.Set<SessionRecord>()
            .AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .Select(x => new SessionDto(x.AccountId, x.RefreshTokenHash, x.ExpiresAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await db.Set<SessionRecord>()
            .Where(x => x.AccountId == accountId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

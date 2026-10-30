using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Modules.Accounts.Application.Dtos;
using VaccinationCard.Api.Modules.Accounts.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Accounts.Application.Security;
using VaccinationCard.Api.Modules.Accounts.Infra.Driven.Persistence.EfCore;

namespace VaccinationCard.Api.Infra.Database.Seeds;

public sealed class AccountSeed(AppDbContext db, IAccountDao dao, IPasswordHasher passwordHasher)
{
    // A public credential of the demo data, not a secret: the account exists only with the seed on (TechSpec §6.4).
    private const string Email = "operador@demo.local";
    private const string Password = "Demo@2026!";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (await db.Set<AccountRecord>().AnyAsync(cancellationToken))
        {
            return;
        }

        await dao.SaveAsync(new AccountCredentialsDto(Guid.CreateVersion7(), Email, passwordHasher.Hash(Password)), cancellationToken);
    }
}

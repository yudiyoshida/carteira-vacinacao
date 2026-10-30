using VaccinationCard.Api.Modules.Accounts.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Accounts.Application.Security;
using VaccinationCard.Api.Modules.Accounts.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Modules.Accounts.Infra.Driven.Security;

namespace VaccinationCard.Api.Modules.Accounts;

public static class AccountsModule
{
    public static IServiceCollection AddAccountsModule(this IServiceCollection services)
    {
        services.AddScoped<IAccountDao, AccountDao>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();

        return services;
    }
}

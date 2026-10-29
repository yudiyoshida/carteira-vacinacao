using VaccinationCard.Api.Modules.Vaccines.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Vaccines.Infra.Driven.Persistence.EfCore;

namespace VaccinationCard.Api.Modules.Vaccines;

public static class VaccinesModule
{
    public static IServiceCollection AddVaccinesModule(this IServiceCollection services)
    {
        services.AddScoped<IVaccineDao, VaccineDao>();

        return services;
    }
}

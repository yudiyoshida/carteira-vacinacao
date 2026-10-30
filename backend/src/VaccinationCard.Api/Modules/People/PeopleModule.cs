using VaccinationCard.Api.Modules.People.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.People.Application.Persistence.Repository;
using VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;

namespace VaccinationCard.Api.Modules.People;

public static class PeopleModule
{
    public static IServiceCollection AddPeopleModule(this IServiceCollection services)
    {
        services.AddScoped<IPersonDao, PersonDao>();
        services.AddScoped<IPersonRepository, PersonRepository>();

        return services;
    }
}

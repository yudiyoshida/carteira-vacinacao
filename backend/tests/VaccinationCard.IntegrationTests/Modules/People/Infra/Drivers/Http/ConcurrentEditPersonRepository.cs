using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Modules.People.Application.Persistence.Repository;
using VaccinationCard.Api.Modules.People.Domain.Entities;
using VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;

namespace VaccinationCard.IntegrationTests.Modules.People.Infra.Drivers.Http;

// Another operation changes the card, in a DI scope of its own, after the request reads the card and before it writes.
// The conflict of RN16 then does not depend on the timing of two real requests.
public sealed class ConcurrentEditPersonRepository(PersonRepository inner, IServiceScopeFactory scopeFactory) : IPersonRepository
{
    public Task<Person?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => inner.FindByIdAsync(id, cancellationToken);

    public Task SaveAsync(Person person, CancellationToken cancellationToken) => inner.SaveAsync(person, cancellationToken);

    public async Task EditAsync(Person person, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<PersonRecord>()
            .Where(x => x.Id == person.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Version, x => x.Version + 1), cancellationToken);

        await inner.EditAsync(person, cancellationToken);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) => inner.DeleteAsync(id, cancellationToken);
}

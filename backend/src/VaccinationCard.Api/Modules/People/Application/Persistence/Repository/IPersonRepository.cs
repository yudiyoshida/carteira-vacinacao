using VaccinationCard.Api.Modules.People.Domain.Entities;

namespace VaccinationCard.Api.Modules.People.Application.Persistence.Repository;

public interface IPersonRepository
{
    Task<Person?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task SaveAsync(Person person, CancellationToken cancellationToken);

    // False when no person has the id.
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

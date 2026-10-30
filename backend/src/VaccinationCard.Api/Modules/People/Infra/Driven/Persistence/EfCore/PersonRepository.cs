using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Modules.People.Application.Persistence.Repository;
using VaccinationCard.Api.Modules.People.Domain.Entities;
using VaccinationCard.Api.Modules.People.Domain.Errors;
using VaccinationCard.Api.Modules.People.Domain.Factories;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;

public sealed class PersonRepository(AppDbContext db) : IPersonRepository
{
    // Unlike the DAO, the read keeps the record tracked in the DbContext of the request, where the writes of the
    // aggregate apply their changes (TechSpec §6.2).
    public async Task<Person?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await db.Set<PersonRecord>().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return record is null ? null : PersonFactory.Load(record.Id, record.Name, Cpf.Create(record.Cpf));
    }

    public async Task SaveAsync(Person person, CancellationToken cancellationToken)
    {
        db.Add(new PersonRecord
        {
            Id = person.Id,
            Name = person.Name,
            Cpf = person.Cpf.Value,
        });

        // The handler checks the CPF first; the unique index settles two registrations that pass that check together.
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation(PersonRecordConfiguration.CpfIndex))
        {
            throw PeopleErrors.CpfAlreadyRegistered();
        }
    }
}

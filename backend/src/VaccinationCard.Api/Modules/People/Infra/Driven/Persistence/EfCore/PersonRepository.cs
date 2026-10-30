using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Modules.People.Application.Persistence.Repository;
using VaccinationCard.Api.Modules.People.Domain.Entities;
using VaccinationCard.Api.Modules.People.Domain.Errors;
using VaccinationCard.Api.Modules.People.Domain.Factories;
using VaccinationCard.Api.Modules.Vaccines.Domain.ValueObjects;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;

public sealed class PersonRepository(AppDbContext db) : IPersonRepository
{
    // Unlike the DAO, the read keeps the records tracked in the DbContext of the request, where the writes of the
    // aggregate apply their changes (TechSpec §6.2).
    public async Task<Person?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await db.Set<PersonRecord>()
            .Include(x => x.Vaccinations)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return record is null ? null : PersonFactory.Load(
            record.Id,
            record.Name,
            Cpf.Create(record.Cpf),
            record.Vaccinations.Select(vaccination => PersonFactory.LoadVaccination(
                vaccination.Id,
                vaccination.VaccineId,
                new Dose(vaccination.DoseType, vaccination.DoseNumber),
                vaccination.AppliedOn
            ))
        );
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

    // Applies to the tracked records what the aggregate changed since FindByIdAsync: the vaccinations it removed, the
    // ones it registered and the dates it corrected. Every edit raises the version, so the write fails if another
    // operation changed the card after that read (RN16).
    public async Task EditAsync(Person person, CancellationToken cancellationToken)
    {
        var record = db.Set<PersonRecord>().Local.Single(x => x.Id == person.Id);
        record.Version++;

        foreach (var removed in record.Vaccinations.Where(x => person.Vaccinations.All(vaccination => vaccination.Id != x.Id)).ToList())
        {
            db.Remove(removed);
        }

        foreach (var vaccination in person.Vaccinations)
        {
            var existing = record.Vaccinations.SingleOrDefault(x => x.Id == vaccination.Id);
            if (existing is null)
            {
                // Add marks the record as new, without relying on EF Core to infer the state from the key.
                db.Add(ToRecord(person.Id, vaccination));
            }
            else
            {
                existing.AppliedOn = vaccination.AppliedOn;
            }
        }

        // The aggregate checks the dose first; the unique index settles two registrations that pass that check together
        // (RN11). A violation comes before the count of rows of the version, so a registration for a person removed
        // after the read fails on the foreign key (TechSpec §6.3).
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw PeopleErrors.ConcurrentUpdate();
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation(VaccinationRecordConfiguration.DoseIndex))
        {
            throw PeopleErrors.DoseAlreadyRegistered();
        }
        catch (DbUpdateException exception) when (exception.IsForeignKeyViolation(VaccinationRecordConfiguration.PersonForeignKey))
        {
            throw PeopleErrors.ConcurrentUpdate();
        }
    }

    // The removal is final, without soft delete (RN04, ADR-0009): one DELETE, without loading the person. The foreign
    // key removes the vaccinations.
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await db.Set<PersonRecord>().Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    private static VaccinationRecord ToRecord(Guid personId, Vaccination vaccination) => new()
    {
        Id = vaccination.Id,
        PersonId = personId,
        VaccineId = vaccination.VaccineId,
        DoseType = vaccination.Dose.Type,
        DoseNumber = (short)vaccination.Dose.Number,
        AppliedOn = vaccination.AppliedOn,
    };
}

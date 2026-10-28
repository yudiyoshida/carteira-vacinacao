# Repository do agregado

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real.

O Repository lê o agregado inteiro, com os registros rastreados no `DbContext` da requisição, e grava aplicando a diferença. O handler sempre lê pelo `FindByIdAsync` antes de chamar o `EditAsync`.

```csharp
namespace VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;

public sealed class PersonRepository(AppDbContext db) : IPersonRepository
{
    public async Task<Person?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await db.Set<PersonRecord>()
            .Include(x => x.Vaccinations)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return record is null ? null : PersonFactory.Load(
            record.Id,
            record.Name,
            Cpf.Create(record.Cpf),
            record.Vaccinations.Select(v =>
                PersonFactory.LoadVaccination(v.Id, v.VaccineId, new Dose(v.DoseType, v.DoseNumber), v.AppliedOn)));
    }

    public async Task EditAsync(Person person, CancellationToken cancellationToken)
    {
        var record = db.Set<PersonRecord>().Local.Single(x => x.Id == person.Id);

        foreach (var removed in record.Vaccinations.Where(v => person.Vaccinations.All(d => d.Id != v.Id)).ToList())
        {
            db.Remove(removed);
        }

        foreach (var vaccination in person.Vaccinations)
        {
            var existing = record.Vaccinations.SingleOrDefault(v => v.Id == vaccination.Id);
            if (existing is null)
            {
                db.Add(ToRecord(person.Id, vaccination));
            }
            else
            {
                existing.AppliedOn = vaccination.AppliedOn;
            }
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation("ux_vaccinations_person_vaccine_dose"))
        {
            throw PeopleErrors.DoseAlreadyRegistered();
        }
    }
}
```

- `db.Add` marca o registro do filho como novo, sem depender de o EF Core deduzir o estado pelo valor da chave.
- Concorrência (T18): `people.version` é token de concorrência; o `EditAsync` incrementa a versão do registro rastreado, e uma `DbUpdateConcurrencyException` vira `PeopleErrors.ConcurrentUpdate()` (TechSpec §6.3).
- `DeleteAsync` usa `ExecuteDeleteAsync` e devolve se removeu alguma linha; a cascata da chave estrangeira apaga as vacinações.

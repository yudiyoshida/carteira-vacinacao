# Repository do agregado

Uma seção cujo código já existe aponta o arquivo real e lista o que observar nele. Uma seção cujo código ainda não existe traz um modelo; quando a task cria o código, a seção passa a apontar o arquivo real.

O Repository lê o agregado inteiro, com os registros rastreados no `DbContext` da requisição, e grava aplicando a diferença. O handler sempre lê pelo `FindByIdAsync` antes de chamar o `EditAsync`.

## Leitura e cadastro

Exemplo real: `backend/src/VaccinationCard.Api/Modules/People/Infra/Driven/Persistence/EfCore/PersonRepository.cs`.

- `FindByIdAsync` lê sem `AsNoTracking`, ao contrário do DAO: o registro fica rastreado para a gravação seguinte. A pessoa volta pelo `PersonFactory.Load`, com o CPF reconstruído por `Cpf.Create`.
- `SaveAsync` grava a pessoa nova e converte a violação de `PersonRecordConfiguration.CpfIndex` em `PeopleErrors.CpfAlreadyRegistered()`, o mesmo erro que o `CreatePersonHandler` lança depois do `ExistsByCpfAsync`. A tradução do `23505` segue o `VaccineDao` (`references/dao.md`).

## Vacinações e edição

Modelo para a T14: a leitura inclui as vacinações, e o `EditAsync` aplica a diferença.

```csharp
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
    catch (DbUpdateException exception) when (exception.IsUniqueViolation(VaccinationRecordConfiguration.DoseIndex))
    {
        throw PeopleErrors.DoseAlreadyRegistered();
    }
}
```

- `db.Add` marca o registro do filho como novo, sem depender de o EF Core deduzir o estado pelo valor da chave.
- Concorrência (T18): `people.version` é token de concorrência; o `EditAsync` incrementa a versão do registro rastreado, e uma `DbUpdateConcurrencyException` vira `PeopleErrors.ConcurrentUpdate()` (TechSpec §6.3).
- `DeleteAsync` (T12) usa `ExecuteDeleteAsync` e devolve se removeu alguma linha; a cascata da chave estrangeira apaga as vacinações.

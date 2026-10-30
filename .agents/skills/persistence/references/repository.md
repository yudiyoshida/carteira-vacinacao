# Repository do agregado

Uma seção cujo código já existe aponta o arquivo real e lista o que observar nele. Uma seção cujo código ainda não existe traz um modelo; quando a task cria o código, a seção passa a apontar o arquivo real.

O Repository lê o agregado inteiro, com os registros rastreados no `DbContext` da requisição, e grava aplicando a diferença. O handler sempre lê pelo `FindByIdAsync` antes de chamar o `EditAsync`.

## Leitura, cadastro e remoção

Exemplo real: `backend/src/VaccinationCard.Api/Modules/People/Infra/Driven/Persistence/EfCore/PersonRepository.cs`.

- `FindByIdAsync` lê sem `AsNoTracking`, ao contrário do DAO: o registro fica rastreado para a gravação seguinte. A pessoa volta pelo `PersonFactory.Load`, com o CPF reconstruído por `Cpf.Create`.
- `SaveAsync` grava a pessoa nova e converte a violação de `PersonRecordConfiguration.CpfIndex` em `PeopleErrors.CpfAlreadyRegistered()`, o mesmo erro que o `CreatePersonHandler` lança depois do `ExistsByCpfAsync`. A tradução do `23505` segue o `VaccineDao` (`references/dao.md`).
- `DeleteAsync` remove com `ExecuteDeleteAsync`, num único `DELETE` e sem carregar a pessoa, e devolve se alguma linha saiu. A cascata da chave estrangeira de `vaccinations.person_id` apaga as vacinações junto (TechSpec §6.1, ADR-0009).

## Vacinações e edição

Exemplo real: `FindByIdAsync` e `EditAsync`, em `backend/src/VaccinationCard.Api/Modules/People/Infra/Driven/Persistence/EfCore/PersonRepository.cs`.

- `FindByIdAsync` inclui as vacinações (`Include`) e reconstrói cada uma pelo `PersonFactory.LoadVaccination`, com a `Dose` montada do tipo e do número do registro.
- `EditAsync` acha o registro rastreado em `Local`, separa as vacinações do agregado que o registro ainda não tem e as adiciona com `db.Add`. O `db.Add` marca o registro do filho como novo, sem depender de o EF Core deduzir o estado pelo valor da chave.
- O `SaveChangesAsync` fica num `try`: a violação de `VaccinationRecordConfiguration.DoseIndex` vira `PeopleErrors.DoseAlreadyRegistered()`. O agregado confere a dose antes, e o índice resolve dois registros da mesma dose que passam juntos por essa checagem (RN11).

Modelo para a T16 e a T17: antes do `SaveChangesAsync`, o `EditAsync` passa a aplicar também a remoção e a correção de data.

```csharp
foreach (var removed in record.Vaccinations.Where(x => person.Vaccinations.All(vaccination => vaccination.Id != x.Id)).ToList())
{
    db.Remove(removed);
}

foreach (var vaccination in person.Vaccinations)
{
    var existing = record.Vaccinations.SingleOrDefault(x => x.Id == vaccination.Id);
    if (existing is not null)
    {
        existing.AppliedOn = vaccination.AppliedOn;
    }
}
```

- Concorrência (T18): `people.version` é token de concorrência; o `EditAsync` incrementa a versão do registro rastreado, e uma `DbUpdateConcurrencyException` vira `PeopleErrors.ConcurrentUpdate()` (TechSpec §6.3).

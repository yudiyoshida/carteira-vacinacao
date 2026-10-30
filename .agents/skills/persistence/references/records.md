# Registro e configuração

Uma seção cujo código já existe aponta o arquivo real e lista o que observar nele. Uma seção cujo código ainda não existe traz um modelo; quando a task cria o código, a seção passa a apontar o arquivo real. Um tipo por arquivo.

## Tabela de módulo CRUD

Exemplo real: `backend/src/VaccinationCard.Api/Modules/Vaccines/Infra/Driven/Persistence/EfCore/VaccineRecord.cs` e `VaccineRecordConfiguration.cs`, na mesma pasta. Migration gerada: `backend/src/VaccinationCard.Api/Infra/Database/Migrations/20261029032547_CreateVaccines.cs`.

- O registro é uma classe `sealed`, com `{ get; set; }` e `required` em toda coluna que o código preenche. `CreatedAt` fica sem `required`, porque o banco de dados preenche.
- A configuração chama `ToTable` com o nome da TechSpec §6.1 e declara as check constraints no segundo argumento, com nome `ck_<tabela>_<coluna>`.
- A chave tem `HasKey` e `ValueGeneratedNever()`, cada texto tem `HasMaxLength`, e `CreatedAt` tem `HasDefaultValueSql("now()")`.
- O nome do índice único fica numa constante pública da configuração (`NormalizedNameIndex`). O `HasDatabaseName` e a tradução do `23505` no DAO usam essa constante.
- A convenção snake_case nomeia as colunas e a chave primária (`primary_doses`, `pk_vaccines`).
- O `AppDbContext` aplica todas as configurações do assembly: um registro novo entra no modelo sem mudar o contexto.

## Filho do agregado

Modelo para a T11 e a T14.

```csharp
public sealed class PersonRecord
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Cpf { get; set; }

    public List<VaccinationRecord> Vaccinations { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
}
```

A configuração de `PersonRecord` declara o índice único do CPF, `ux_people_cpf`, com o nome na constante `CpfIndex`.

A configuração de `VaccinationRecord` declara:

- `person_id` com `OnDelete(DeleteBehavior.Cascade)` e `vaccine_id` com `OnDelete(DeleteBehavior.Restrict)`;
- `dose_type` como texto, com `HasConversion<string>()`;
- o índice único da dose, `ux_vaccinations_person_vaccine_dose`, sobre `(person_id, vaccine_id, dose_type, dose_number)`, com o nome na constante `DoseIndex`.

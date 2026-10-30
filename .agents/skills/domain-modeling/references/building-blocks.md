# Blocos do domínio

Uma seção cujo código já existe aponta o arquivo real e lista o que observar nele. Uma seção cujo código ainda não existe traz um modelo; quando a task cria o código, a seção passa a apontar o arquivo real. Um tipo por arquivo.

## Value object

Exemplo real: `backend/src/VaccinationCard.Api/Modules/Vaccines/Domain/ValueObjects/DoseScheme.cs`.

- O value object é um `sealed record` imutável, com igualdade por valor e propriedades só com `get`.
- O construtor guarda as invariantes antes de atribuir as propriedades, com `ArgumentOutOfRangeException.ThrowIfLessThan`, `ThrowIfGreaterThan` e `ThrowIfNegative`.
- Os limites são constantes públicas (`MaxPrimaryDoses`, `MaxBoosterDoses`), que o validator também usa.
- A guarda pega erro de programação: o validator já barrou a entrada do operador com `400`, como registra o comentário da classe.

## Entidade

Modelo para a T11 e a T13. Estado privado, mudado só pelos métodos que aplicam as regras.

```csharp
namespace VaccinationCard.Api.Modules.People.Domain.Entities;

public sealed class Person
{
    private readonly List<Vaccination> _vaccinations;

    internal Person(Guid id, string name, Cpf cpf, IEnumerable<Vaccination> vaccinations)
    {
        Id = id;
        Name = name;
        Cpf = cpf;
        _vaccinations = [.. vaccinations];
    }

    public Guid Id { get; }

    public string Name { get; }

    public Cpf Cpf { get; }

    public IReadOnlyList<Vaccination> Vaccinations => _vaccinations;

    public Vaccination RegisterVaccination(Guid vaccineId, DoseScheme scheme, Dose dose, DateOnly appliedOn, DateOnly today)
    {
        if (!scheme.Contains(dose))
        {
            throw PeopleErrors.DoseOutsideScheme();
        }

        // Demais regras na ordem de references/person-aggregate.md.

        var vaccination = new Vaccination(Guid.CreateVersion7(), vaccineId, dose, appliedOn);
        _vaccinations.Add(vaccination);
        return vaccination;
    }
}
```

## Factory

Modelo para a T11. `Create` para a entidade nova; `Load` para reconstruir a entidade lida do banco de dados.

```csharp
namespace VaccinationCard.Api.Modules.People.Domain.Factories;

public static class PersonFactory
{
    public static Person Create(string name, Cpf cpf)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Person(Guid.CreateVersion7(), name, cpf, []);
    }

    public static Person Load(Guid id, string name, Cpf cpf, IEnumerable<Vaccination> vaccinations)
    {
        return new Person(id, name, cpf, vaccinations);
    }

    public static Vaccination LoadVaccination(Guid id, Guid vaccineId, Dose dose, DateOnly appliedOn)
    {
        return new Vaccination(id, vaccineId, dose, appliedOn);
    }
}
```

# Blocos do domínio

Modelo. Quando o repositório já tiver um exemplo real do bloco, siga o exemplo real. Um tipo por arquivo.

## Value object

Imutável, com igualdade por valor, validado no construtor.

```csharp
namespace VaccinationCard.Api.Modules.Vaccines.Domain.ValueObjects;

public sealed record DoseScheme
{
    public const int MaxPrimaryDoses = 10;
    public const int MaxBoosterDoses = 5;

    public DoseScheme(int primaryDoses, int boosterDoses)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(primaryDoses, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(primaryDoses, MaxPrimaryDoses);
        ArgumentOutOfRangeException.ThrowIfNegative(boosterDoses);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(boosterDoses, MaxBoosterDoses);

        PrimaryDoses = primaryDoses;
        BoosterDoses = boosterDoses;
    }

    public int PrimaryDoses { get; }

    public int BoosterDoses { get; }
}
```

O validator usa as mesmas constantes para os limites da entrada.

## Entidade

Estado privado, mudado só pelos métodos que aplicam as regras.

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

`Create` para a entidade nova; `Load` para reconstruir a entidade lida do banco de dados.

```csharp
namespace VaccinationCard.Api.Modules.People.Domain.Factories;

public static class PersonFactory
{
    public static Person Create(string name, Cpf cpf)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Person(Guid.CreateVersion7(), name, cpf, []);
    }

    public static Person Load(Guid id, string name, Cpf cpf, IEnumerable<Vaccination> vaccinations) =>
        new(id, name, cpf, vaccinations);

    public static Vaccination LoadVaccination(Guid id, Guid vaccineId, Dose dose, DateOnly appliedOn) =>
        new(id, vaccineId, dose, appliedOn);
}
```

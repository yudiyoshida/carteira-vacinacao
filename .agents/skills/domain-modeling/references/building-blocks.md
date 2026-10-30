# Blocos do domínio

Uma seção cujo código já existe aponta o arquivo real e lista o que observar nele. Uma seção cujo código ainda não existe traz um modelo; quando a task cria o código, a seção passa a apontar o arquivo real. Um tipo por arquivo.

## Value object

Exemplo real: `backend/src/VaccinationCard.Api/Modules/Vaccines/Domain/ValueObjects/DoseScheme.cs`.

- O value object é um `sealed record` imutável, com igualdade por valor e propriedades só com `get`.
- O construtor guarda as invariantes antes de atribuir as propriedades, com `ArgumentOutOfRangeException.ThrowIfLessThan`, `ThrowIfGreaterThan` e `ThrowIfNegative`.
- Os limites são constantes públicas (`MaxPrimaryDoses`, `MaxBoosterDoses`), que o validator também usa.
- A guarda pega erro de programação: o validator já barrou a entrada do operador com `400`, como registra o comentário da classe.

## Value object lido de texto

Exemplo real: `backend/src/VaccinationCard.Api/Shared/ValueObjects/Cpf.cs`.

- O construtor é privado. `IsValid(string?)` atende o validator, e `Create(string)` aplica a mesma checagem como guarda, com `ArgumentException`.
- A entrada aceita mais de uma forma: pontos, hífens e espaços caem em qualquer posição, e restam 11 dígitos (TechSpec §5.2). O valor guardado é a forma normalizada (`Value`, só os dígitos), e as formas de exibição são propriedades calculadas (`Formatted` e `Masked`). A Application escolhe a forma de cada saída (RN26).
- O `ToString` devolve a forma mascarada, para que um CPF que chegue a um log por acidente saia mascarado (RN25).

## Entidade

Exemplo real: `backend/src/VaccinationCard.Api/Modules/People/Domain/Entities/Person.cs`.

- O construtor é `internal`, e só a factory o chama. As propriedades têm só `get`.
- Os limites da regra do nome são constantes públicas (`MinNameLength` e `MaxNameLength`, RN01), que o validator e a guarda da factory usam.

Modelo para a T13: a lista de vacinações e os métodos que aplicam as regras. O estado é privado e muda só por esses métodos.

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

Exemplo real: `backend/src/VaccinationCard.Api/Modules/People/Domain/Factories/PersonFactory.cs`.

- `Create` cria a entidade nova: guarda o nome com `ArgumentException.ThrowIfNullOrWhiteSpace` e com os limites de `Person`, passando `nameof(name)` às guardas de tamanho, e gera o id com `Guid.CreateVersion7()`.
- `Load` reconstrói a entidade lida do banco de dados, sem validar; só o Repository o chama.

Modelo para a T13 e a T14: o `Create` passa uma lista vazia ao construtor, o `Load` recebe as vacinações, e o `LoadVaccination` reconstrói cada uma.

```csharp
public static class PersonFactory
{
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

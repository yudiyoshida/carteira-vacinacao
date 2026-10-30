# Blocos do domínio

Uma seção cujo código já existe aponta o arquivo real e lista o que observar nele. Uma seção cujo código ainda não existe traz um modelo; quando a task cria o código, a seção passa a apontar o arquivo real. Um tipo por arquivo.

## Value object

Exemplo real: `backend/src/VaccinationCard.Api/Modules/Vaccines/Domain/ValueObjects/DoseScheme.cs`.

- O value object é um `sealed record` imutável, com igualdade por valor e propriedades só com `get`.
- O construtor guarda as invariantes antes de atribuir as propriedades, com `ArgumentOutOfRangeException.ThrowIfLessThan`, `ThrowIfGreaterThan` e `ThrowIfNegative`.
- Os limites são constantes públicas (`MaxPrimaryDoses`, `MaxBoosterDoses`), que o validator também usa.
- A guarda pega erro de programação: o validator já barrou a entrada do operador com `400`, como registra o comentário da classe.
- As operações que respondem pelo próprio valor ficam no value object: `Contains(dose)` (RN09) e `OrderedDoses()`, a sequência de doses (RN10).

## Value object com ordem

Exemplo real: `backend/src/VaccinationCard.Api/Modules/Vaccines/Domain/ValueObjects/Dose.cs`.

- `Dose` implementa `IComparable<Dose>` com a ordem da sequência (RN10): as doses primárias antes dos reforços, e cada tipo pelo número. A comparação nomeia o tipo que vem primeiro, sem depender do valor numérico do enum.
- A guarda do tipo usa `Enum.IsDefined`, porque um número fora do enum (`(DoseType)99`) também é um `DoseType`.

## Value object lido de texto

Exemplo real: `backend/src/VaccinationCard.Api/Shared/ValueObjects/Cpf.cs`.

- O construtor é privado. `IsValid(string?)` atende o validator, e `Create(string)` aplica a mesma checagem como guarda, com `ArgumentException`.
- A entrada aceita mais de uma forma: pontos, hífens e espaços caem em qualquer posição, e restam 11 dígitos (TechSpec §5.2). O valor guardado é a forma normalizada (`Value`, só os dígitos), e as formas de exibição são propriedades calculadas (`Formatted` e `Masked`). A Application escolhe a forma de cada saída (RN26).
- O `ToString` devolve a forma mascarada, para que um CPF que chegue a um log por acidente saia mascarado (RN25).

## Entidade

Exemplos reais: `backend/src/VaccinationCard.Api/Modules/People/Domain/Entities/Person.cs` e `backend/src/VaccinationCard.Api/Modules/People/Domain/Entities/Vaccination.cs`.

- O construtor é `internal`, e só a factory o chama (`Vaccination` nasce pelo `RegisterVaccination`). As propriedades têm só `get`.
- Os limites de uma regra são constantes públicas na entidade dona do valor: `Person.MinNameLength` e `Person.MaxNameLength` (RN01), usados pelo validator e pela guarda da factory, e `Vaccination.MinAppliedOn` (RN12).
- A lista de vacinações é privada (`_vaccinations`) e sai como `IReadOnlyList<Vaccination>`. O estado muda só pelos métodos do agregado.
- `RegisterVaccination` checa uma regra por bloco, na ordem do PRD (RN09, RN11, RN10, RN12, RN13), e lança pelo catálogo; só o primeiro erro aparece. RN12 fica num helper privado (`CheckAppliedOn`), que a correção de data (T16) também usa.
- `NextExpectedDose` se apoia no prefixo da sequência (TechSpec §5.2): a próxima dose é `OrderedDoses()[n]`, com `n` igual ao número de vacinações da vacina.
- A última dose de uma vacina sai de `MaxBy` pela ordem da `Dose`, não pela posição na lista, que não tem ordem garantida quando vem do banco de dados.

## Factory

Exemplo real: `backend/src/VaccinationCard.Api/Modules/People/Domain/Factories/PersonFactory.cs`.

- `Create` cria a entidade nova: guarda o nome com `ArgumentException.ThrowIfNullOrWhiteSpace` e com os limites de `Person`, passando `nameof(name)` às guardas de tamanho, e gera o id com `Guid.CreateVersion7()`.
- `Load` reconstrói a entidade lida do banco de dados, sem validar; só o Repository o chama.

Modelo para a T14: o construtor de `Person` passa a receber as vacinações, o `Create` passa uma lista vazia, o `Load` recebe as vacinações, e o `LoadVaccination` reconstrói cada uma. Até lá, a lista nasce vazia no próprio campo (`_vaccinations = []`).

```csharp
internal Person(Guid id, string name, Cpf cpf, IEnumerable<Vaccination> vaccinations)
{
    Id = id;
    Name = name;
    Cpf = cpf;
    _vaccinations = [.. vaccinations];
}
```

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

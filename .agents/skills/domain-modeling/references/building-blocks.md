# Blocos do domínio

Uma seção cujo código já existe aponta o arquivo real e lista o que observar nele. Uma seção cujo código ainda não existe traz um modelo; quando a task cria o código, a seção passa a apontar o arquivo real. Um tipo por arquivo.

## Value object

Exemplo real: `backend/src/VaccinationCard.Api/Modules/Vaccines/Domain/ValueObjects/DoseScheme.cs`.

- O value object é um `sealed record` imutável, com igualdade por valor e propriedades só com `get`.
- O construtor guarda as invariantes antes de atribuir as propriedades, com `ArgumentOutOfRangeException.ThrowIfLessThan`, `ThrowIfGreaterThan` e `ThrowIfNegative`.
- Os limites são constantes públicas (`MaxPrimaryDoses`, `MaxBoosterDoses`), que o validator também usa.
- A guarda pega erro de programação: o validator já barrou a entrada do operador com `400`, como registra o comentário da classe.
- As operações que respondem pelo próprio valor ficam no value object: `Contains(dose)` (RN09), `OrderedDoses()`, a sequência de doses (RN10), e `NextExpectedDose(registeredDoses)`, a dose que segue um prefixo da sequência (RN10).
- Uma regra que o agregado e uma leitura usam fica no value object, para as duas seguirem o mesmo código: o `Person.NextExpectedDose` e o cartão de vacinação (`FindVaccinationCardHandler`) chamam o `DoseScheme.NextExpectedDose`.

## Value object com ordem

Exemplo real: `backend/src/VaccinationCard.Api/Modules/Vaccines/Domain/ValueObjects/Dose.cs`.

- `Dose` implementa `IComparable<Dose>` com a ordem da sequência (RN10): as doses primárias antes dos reforços, e cada tipo pelo número. A comparação nomeia o tipo que vem primeiro, sem depender do valor numérico do enum.
- A guarda do tipo usa `Enum.IsDefined`, porque um número fora do enum (`(DoseType)99`) também é um `DoseType`.

## Enum

Exemplo real: `backend/src/VaccinationCard.Api/Modules/Vaccines/Domain/Enums/DoseType.cs`.

- Um enum que chega no corpo de uma requisição começa em 1 (`Primary = 1`, `Booster = 2`). Um campo ausente chega com o valor 0, que o enum não define, e o `IsInEnum` do validator recusa (RN08).
- O banco de dados e o JSON usam o nome do valor, e nenhum código depende do número, como a comparação de `Dose`.

## Value object lido de texto

Exemplo real: `backend/src/VaccinationCard.Api/Shared/ValueObjects/Cpf.cs`.

- O construtor é privado. `IsValid(string?)` atende o validator, e `Create(string)` aplica a mesma checagem como guarda, com `ArgumentException`.
- A entrada aceita mais de uma forma: pontos, hífens e espaços caem em qualquer posição, e restam 11 dígitos (TechSpec §5.2). O valor guardado é a forma normalizada (`Value`, só os dígitos), e as formas de exibição são propriedades calculadas (`Formatted` e `Masked`). A Application escolhe a forma de cada saída (RN26).
- O `ToString` devolve a forma mascarada, para que um CPF que chegue a um log por acidente saia mascarado (RN25).

## Entidade

Exemplos reais: `backend/src/VaccinationCard.Api/Modules/People/Domain/Entities/Person.cs` e `backend/src/VaccinationCard.Api/Modules/People/Domain/Entities/Vaccination.cs`.

- O construtor é `internal`, e só a factory o chama (`Vaccination` nasce pelo `RegisterVaccination` ou, lida do banco de dados, pelo `PersonFactory.LoadVaccination`). As propriedades têm só `get`, exceto `Vaccination.AppliedOn`, com `private set`: a data muda só pelo `CorrectAppliedOn`, um método `internal` que a raiz chama depois de conferir as regras (RN14).
- Os limites de uma regra são constantes públicas na entidade dona do valor: `Person.MinNameLength` e `Person.MaxNameLength` (RN01), usados pelo validator e pela guarda da factory, e `Vaccination.MinAppliedOn` (RN12).
- A lista de vacinações é privada (`_vaccinations`): o construtor copia as vacinações recebidas (`[.. vaccinations]`), e a lista sai como `IReadOnlyList<Vaccination>`. O estado muda só pelos métodos do agregado.
- `RegisterVaccination` checa uma regra por bloco, na ordem do PRD (RN09, RN11, RN10, RN12, RN13), e lança pelo catálogo; só o primeiro erro aparece. RN12 fica num helper privado (`CheckAppliedOn`), que a correção de data também usa.
- `CorrectVaccinationDate` localiza a vacinação e checa RN12 antes de RN14. As doses vizinhas saem das vacinações da mesma vacina, ordenadas pela `Dose`: o `ElementAtOrDefault` nas posições ao lado devolve `null` quando a vizinha não existe.
- `NextExpectedDose` conta as vacinações da vacina e delega ao `DoseScheme.NextExpectedDose`, que se apoia no prefixo da sequência (TechSpec §5.2): a próxima dose é `OrderedDoses()[n]`, com `n` igual ao número de vacinações da vacina.
- A última dose de uma vacina sai de `MaxBy` pela ordem da `Dose`, não pela posição na lista, que não tem ordem garantida quando vem do banco de dados.

## Factory

Exemplo real: `backend/src/VaccinationCard.Api/Modules/People/Domain/Factories/PersonFactory.cs`.

- `Create` cria a entidade nova: guarda o nome com `ArgumentException.ThrowIfNullOrWhiteSpace` e com os limites de `Person`, passando `nameof(name)` às guardas de tamanho, gera o id com `Guid.CreateVersion7()` e passa uma lista vazia de vacinações.
- `Load` reconstrói a pessoa lida do banco de dados, com as vacinações, e `LoadVaccination` reconstrói cada vacinação. Os dois não validam, e só o Repository os chama: as regras já valeram quando cada vacinação foi registrada.

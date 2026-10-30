# Teste unitário

Uma seção cujo código já existe aponta o arquivo real e lista o que observar nele. Uma seção cujo código ainda não existe traz um modelo; quando a task cria o código, a seção passa a apontar o arquivo real.

## Domínio

Exemplo real: `backend/tests/VaccinationCard.UnitTests/Modules/People/Domain/Entities/PersonTests.cs`. Os casos de cada regra estão na skill `domain-modeling`, em `references/person-aggregate.md`.

- "Hoje" é a constante `Today`, passada ao método como parâmetro: o domínio não lê o relógio, e o teste dispensa o `FakeTimeProvider`.
- A vacina, os esquemas e as doses são campos `static readonly` com o nome do caso (`ThreeDoses`, `TwoDosesAndTwoBoosters`, `FirstDose`, `FirstBooster`), e o `_sut` nasce pela `PersonFactory.Create`.
- O Arrange monta as doses anteriores pelo próprio `RegisterVaccination`, com o helper `RegisterDose`, e o Act chama o método direto.
- Uma pessoa lida do banco de dados nasce pelo `PersonFactory.Load`, com as vacinações do `PersonFactory.LoadVaccination`. O teste das regras sobre o estado lido usa essa pessoa numa variável local, no lugar do `_sut`.
- Os testes seguem a ordem de checagem. Um teste de ordem viola duas regras, leva os dois `Trait` e confere que só o código da primeira aparece.
- O erro se confere pelo tipo e pelo `Code`: `Should.Throw<BusinessRuleException>(act)` e `exception.Code.ShouldBe("vaccination.dose_out_of_sequence")`; a dose repetida (RN11) é `ConflictException`.

## Value object

Exemplo real: `backend/tests/VaccinationCard.UnitTests/Modules/Vaccines/Domain/ValueObjects/DoseSchemeTests.cs`.

- Os limites aceitos, inferior e superior, têm testes próprios, com os valores em `const` no Arrange.
- Os valores fora do limite ficam num `Theory` por campo, com um caso para cada guarda.
- A guarda se confere pelo tipo da exceção e pelo parâmetro: `Should.Throw<ArgumentOutOfRangeException>(act)` e `exception.ParamName.ShouldBe(nameof(primaryDoses))`. A guarda pega erro de programação, então não tem código de erro.
- Uma operação do value object tem um `Theory` para os casos aceitos e outro para os recusados, como `Contains`.

Value object com ordem: `backend/tests/VaccinationCard.UnitTests/Modules/Vaccines/Domain/ValueObjects/DoseTests.cs`. A ordem se prova ordenando uma lista embaralhada com `Order()` e comparando com a lista esperada, e a igualdade por valor tem teste próprio (RN08).

Value object lido de texto: `backend/tests/VaccinationCard.UnitTests/Shared/ValueObjects/CpfTests.cs`.

- O Arrange monta a entrada a partir de um CPF válido (`123.456.789-09`), trocando os dígitos verificadores, um separador ou o tamanho, para o caso mostrar o que muda.
- Cada sequência de um dígito repetido passa no cálculo dos dígitos verificadores e tem caso próprio.
- `Formatted`, `Masked` e `ToString` conferem a forma exata de cada saída (RN26, RN25).

## Factory

Exemplo real: `backend/tests/VaccinationCard.UnitTests/Modules/People/Domain/Factories/PersonFactoryTests.cs`.

- Os limites do nome se provam com o tamanho no `Theory` e o texto gerado no Arrange: `new string('A', length)`.
- `Create` confere o id versão 7; `Load` confere que o id dado se mantém.

## Relógio

Exemplo real: `backend/tests/VaccinationCard.UnitTests/Core/Time/ClockTests.cs`.

- O `FakeTimeProvider` fica num campo, e cada teste fixa o instante com `SetUtcNow`.
- Os casos ficam dos dois lados da meia-noite de Brasília: às 02h30 UTC ainda é o dia anterior (23h30 em Brasília), e às 03h00 UTC já é o dia seguinte (RN12).

## Handler com ports mockados

Exemplo real: `backend/tests/VaccinationCard.UnitTests/Modules/Vaccines/Application/UseCases/CreateVaccine/CreateVaccineHandlerTests.cs`.

- O port mockado fica num campo (`Substitute.For<IVaccineDao>()`), e o construtor do teste cria o `_sut` com `=>`.
- O retorno do mock se define no Arrange: `_dao.ExistsByNameAsync(input.Name, Arg.Any<CancellationToken>()).Returns(true)`.
- O efeito se confere no port: `Received(1)` com os argumentos esperados, `Arg.Is<VaccineDto>(vaccine => vaccine.Name == "Hepatite B")` para conferir um campo só e `DidNotReceive()` quando o handler para antes de gravar.
- Um handler que só repassa ao DAO confere os argumentos repassados, como o padrão de página e a busca aparada em `backend/tests/VaccinationCard.UnitTests/Modules/Vaccines/Application/UseCases/FindAllVaccines/FindAllVaccinesHandlerTests.cs`.
- Um handler que consulta outro módulo recebe o `ISender` mockado, com o retorno definido pela query: `_sender.Send(new FindVaccineByIdQuery(id), Arg.Any<CancellationToken>()).Returns(vaccine)`. A falha do outro módulo usa `ThrowsAsync`, de `NSubstitute.ExceptionExtensions`. Exemplo: `backend/tests/VaccinationCard.UnitTests/Modules/People/Application/UseCases/RegisterVaccination/RegisterVaccinationHandlerTests.cs`.
- Uma query com uma coleção, como a `FindVaccinesByIdsQuery`, não se compara por valor: o record compara a referência da lista. O mock responde a qualquer query da classe (`Arg.Any<FindVaccinesByIdsQuery>()`), e o teste dos argumentos confere a lista com `Received(1)` e `Arg.Is<FindVaccinesByIdsQuery>(query => query.Ids.Count == 2 && ...)`.
- Um handler que monta uma resposta de dois ports prepara os dois num helper do Arrange (`ArrangeCard`), e cada teste confere uma parte da resposta: ordem, doses, progresso, próxima dose. Exemplo: `backend/tests/VaccinationCard.UnitTests/Modules/People/Application/UseCases/FindVaccinationCard/FindVaccinationCardHandlerTests.cs`.
- Um DTO com uma lista também compara a lista por referência, então o teste confere as partes: os DTOs sem lista (`Progress`, `Scheme`) com `ShouldBe`, e a lista com `ShouldBe([...])`, item a item.
- O `IClock` mockado devolve uma data passada: um handler que lesse o relógio real aceitaria o dia seguinte a ela, e o teste da data futura falharia.
- Um handler que lê o retorno do DAO quebraria com o `null` que o mock devolve por padrão para um record selado. O construtor do teste define um retorno padrão com `ReturnsForAnyArgs`, e o teste que confere a saída define o retorno para os argumentos dele: `backend/tests/VaccinationCard.UnitTests/Modules/People/Application/UseCases/FindAllPeople/FindAllPeopleHandlerTests.cs`.

## Validator

Exemplos reais: `backend/tests/VaccinationCard.UnitTests/Modules/Vaccines/Application/UseCases/CreateVaccine/CreateVaccineValidatorTests.cs` e `backend/tests/VaccinationCard.UnitTests/Modules/Vaccines/Application/UseCases/FindAllVaccines/FindAllVaccinesValidatorTests.cs`.

- O `_sut` nasce com `new()`, e um teste confere a entrada válida inteira: `result.IsValid.ShouldBeTrue()`.
- Cada limite tem o caso aceito e o recusado. A recusa confere o campo: `result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateVaccineCommand.PrimaryDoses))`. O campo de um objeto aninhado tem o caminho: `"Dose.Type"`.
- Um campo aparado tem casos com espaços nas pontas, aceitos e recusados. O texto gerado, como `new string('A', 100)`, vai num `TheoryData`.

## Log

Exemplo real: `backend/tests/VaccinationCard.UnitTests/Infra/Http/ProblemDetailsExceptionHandlerTests.cs`.

- O `FakeLogger<T>` fica num campo e entra no construtor do objeto testado.
- O teste confere o nível e os campos estruturados de `_logger.LatestRecord`, não o texto: `record.Level.ShouldBe(LogLevel.Error)` e `record.GetStructuredStateValue("Code").ShouldBe("internal.unexpected")`.

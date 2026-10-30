# Teste unitário

Uma seção cujo código já existe aponta o arquivo real e lista o que observar nele. Uma seção cujo código ainda não existe traz um modelo; quando a task cria o código, a seção passa a apontar o arquivo real.

## Domínio

Modelo para a T13.

```csharp
namespace VaccinationCard.UnitTests.Modules.People.Domain.Entities;

public sealed class PersonTests
{
    private static readonly DateOnly Today = new(2026, 10, 28);
    private static readonly Guid VaccineId = Guid.CreateVersion7();
    private static readonly DoseScheme ThreeDoses = new(3, 0);

    private readonly Person _sut = PersonFactory.Create("Maria Silva", Cpf.Create("12345678909"));

    [Fact]
    [Trait("RN", "RN10")]
    public void Should_Reject_Dose_When_Previous_Dose_Is_Missing()
    {
        // Arrange
        var secondDose = new Dose(DoseType.Primary, 2);

        // Act
        var act = () => _sut.RegisterVaccination(VaccineId, ThreeDoses, secondDose, Today, Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.dose_out_of_sequence");
    }
}
```

## Value object

Exemplo real: `backend/tests/VaccinationCard.UnitTests/Modules/Vaccines/Domain/ValueObjects/DoseSchemeTests.cs`.

- Os limites aceitos, inferior e superior, têm testes próprios, com os valores em `const` no Arrange.
- Os valores fora do limite ficam num `Theory` por campo, com um caso para cada guarda.
- A guarda se confere pelo tipo da exceção e pelo parâmetro: `Should.Throw<ArgumentOutOfRangeException>(act)` e `exception.ParamName.ShouldBe(nameof(primaryDoses))`. A guarda pega erro de programação, então não tem código de erro.

Value object lido de texto: `backend/tests/VaccinationCard.UnitTests/Shared/ValueObjects/CpfTests.cs`.

- O Arrange monta a entrada a partir de um CPF válido (`123.456.789-09`), trocando os dígitos verificadores, um separador ou o tamanho, para o caso mostrar o que muda.
- Cada sequência de um dígito repetido passa no cálculo dos dígitos verificadores e tem caso próprio.
- `Formatted`, `Masked` e `ToString` conferem a forma exata de cada saída (RN26, RN25).

## Factory

Exemplo real: `backend/tests/VaccinationCard.UnitTests/Modules/People/Domain/Factories/PersonFactoryTests.cs`.

- Os limites do nome se provam com o tamanho no `Theory` e o texto gerado no Arrange: `new string('A', length)`.
- `Create` confere o id versão 7; `Load` confere que o id dado se mantém.

## Handler com ports mockados

Exemplo real: `backend/tests/VaccinationCard.UnitTests/Modules/Vaccines/Application/UseCases/CreateVaccine/CreateVaccineHandlerTests.cs`.

- O port mockado fica num campo (`Substitute.For<IVaccineDao>()`), e o construtor do teste cria o `_sut` com `=>`.
- O retorno do mock se define no Arrange: `_dao.ExistsByNameAsync(input.Name, Arg.Any<CancellationToken>()).Returns(true)`.
- O efeito se confere no port: `Received(1)` com os argumentos esperados, `Arg.Is<VaccineDto>(vaccine => vaccine.Name == "Hepatite B")` para conferir um campo só e `DidNotReceive()` quando o handler para antes de gravar.
- Um handler que só repassa ao DAO confere os argumentos repassados, como o padrão de página e a busca aparada em `backend/tests/VaccinationCard.UnitTests/Modules/Vaccines/Application/UseCases/FindAllVaccines/FindAllVaccinesHandlerTests.cs`.
- Um handler que lê o retorno do DAO quebraria com o `null` que o mock devolve por padrão para um record selado. O construtor do teste define um retorno padrão com `ReturnsForAnyArgs`, e o teste que confere a saída define o retorno para os argumentos dele: `backend/tests/VaccinationCard.UnitTests/Modules/People/Application/UseCases/FindAllPeople/FindAllPeopleHandlerTests.cs`.

## Validator

Exemplos reais: `backend/tests/VaccinationCard.UnitTests/Modules/Vaccines/Application/UseCases/CreateVaccine/CreateVaccineValidatorTests.cs` e `backend/tests/VaccinationCard.UnitTests/Modules/Vaccines/Application/UseCases/FindAllVaccines/FindAllVaccinesValidatorTests.cs`.

- O `_sut` nasce com `new()`, e um teste confere a entrada válida inteira: `result.IsValid.ShouldBeTrue()`.
- Cada limite tem o caso aceito e o recusado. A recusa confere o campo: `result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateVaccineCommand.PrimaryDoses))`.
- Um campo aparado tem casos com espaços nas pontas, aceitos e recusados. O texto gerado, como `new string('A', 100)`, vai num `TheoryData`.

## Log

Exemplo real: `backend/tests/VaccinationCard.UnitTests/Infra/Http/ProblemDetailsExceptionHandlerTests.cs`.

- O `FakeLogger<T>` fica num campo e entra no construtor do objeto testado.
- O teste confere o nível e os campos estruturados de `_logger.LatestRecord`, não o texto: `record.Level.ShouldBe(LogLevel.Error)` e `record.GetStructuredStateValue("Code").ShouldBe("internal.unexpected")`.

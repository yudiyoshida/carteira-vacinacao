# Exceções e catálogo de erros

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real.

## Exceção de categoria (`Shared/Errors`)

As quatro exceções têm o mesmo formato e nenhuma classe base comum:

```csharp
namespace VaccinationCard.Api.Shared.Errors;

public sealed class BusinessRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
```

## Catálogo do módulo (`Domain/Errors/<Módulo>Errors.cs`)

Um método por erro. O método devolve a exceção da categoria certa, e quem lança só escolhe o erro:

```csharp
namespace VaccinationCard.Api.Modules.People.Domain.Errors;

public static class PeopleErrors
{
    public static NotFoundException PersonNotFound() =>
        new("person.not_found", "Pessoa não encontrada.");

    public static ConflictException CpfAlreadyRegistered() =>
        new("person.cpf_already_registered", "Já existe uma pessoa cadastrada com este CPF.");

    public static BusinessRuleException DoseOutOfSequence() =>
        new("vaccination.dose_out_of_sequence", "A dose informada não é a próxima dose esperada desta vacina.");
}
```

Uso: `throw PeopleErrors.DoseOutOfSequence();`. O código e a mensagem vêm da TechSpec §8.3, sem variação.

## Conversão em HTTP (`Infra/Http`)

O `IExceptionHandler` escolhe o status pelo tipo da exceção e monta o ProblemDetails com `title`, `status`, `detail` (a mensagem), `instance`, `code` e `traceId` (TechSpec §8.1). A `ValidationException` do FluentValidation vira `400`, com `code` `validation.failed` e `errors` por campo, em camelCase.

## Teste

```csharp
var exception = Should.Throw<BusinessRuleException>(act);
exception.Code.ShouldBe("vaccination.dose_out_of_sequence");
```

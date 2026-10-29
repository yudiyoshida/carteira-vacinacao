# Exceções e catálogo de erros

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real.

## Exceção de categoria (`Shared/Errors`)

As quatro exceções têm o mesmo formato e nenhuma classe base comum. Exemplo real: `backend/src/VaccinationCard.Api/Shared/Errors/BusinessRuleException.cs`.

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

Código real em `backend/src/VaccinationCard.Api/Infra/Http/`:

| Peça | O que faz |
|---|---|
| `ApiProblemDetails` | monta o corpo da TechSpec §8.1: `title` pela tabela de status da §8.2, `detail`, `instance`, `code` e `traceId`; `CreateValidation` converte cada campo para o nome do JSON (`Dose.Number` → `dose.number`); `WriteAsync` grava como `application/problem+json`, qualquer que seja o `Accept` |
| `ProblemDetailsExceptionHandler` | `IExceptionHandler`: escolhe o status pelo tipo da exceção; a `ValidationException` do FluentValidation vira `400` `validation.failed`; outra exceção vira `500` `internal.unexpected`, com log `Error` |
| `InvalidModelStateResponse` | `400` `validation.failed` quando o MVC não consegue ler o corpo ou um parâmetro |
| `StatusCodeProblemDetails` | corpo dos erros que o roteamento e o MVC dão sem corpo: `route.not_found`, `route.method_not_allowed` e `request.unsupported_media_type` |

Uma falha que não vem de exceção usa a mesma fábrica. Exemplo do `OnChallenge` do JwtBearer:

```csharp
OnChallenge = async context =>
{
    context.HandleResponse();
    var problem = ApiProblemDetails.Create(
        context.HttpContext, StatusCodes.Status401Unauthorized, "auth.unauthenticated", "Autenticação necessária.");
    await ApiProblemDetails.WriteAsync(context.HttpContext, problem, context.HttpContext.RequestAborted);
},
```

## Teste

```csharp
var exception = Should.Throw<BusinessRuleException>(act);
exception.Code.ShouldBe("vaccination.dose_out_of_sequence");
```

# Exceções e catálogo de erros

Uma seção cujo código já existe aponta o arquivo real e lista o que observar nele. Uma seção cujo código ainda não existe traz um modelo; quando a task cria o código, a seção passa a apontar o arquivo real.

## Exceção de categoria (`Shared/Errors`)

Exemplo real: `backend/src/VaccinationCard.Api/Shared/Errors/`.

- As quatro exceções têm o mesmo formato e nenhuma classe base comum: `public sealed class BusinessRuleException(string code, string message) : Exception(message)`, com a propriedade `Code`.
- A exceção não conhece HTTP: o status sai do tipo dela, na conversão em `Infra/Http`.

## Catálogo do módulo (`Domain/Errors/<Módulo>Errors.cs`)

Exemplos reais: `backend/src/VaccinationCard.Api/Modules/Vaccines/Domain/Errors/VaccinesErrors.cs` e `backend/src/VaccinationCard.Api/Modules/People/Domain/Errors/PeopleErrors.cs`.

- O catálogo é uma classe estática com um método por erro. O método devolve a exceção da categoria certa, e quem lança só escolhe o erro: `throw VaccinesErrors.NameAlreadyRegistered();`.
- O método tem corpo em bloco e o nome do motivo: `VaccineNotFound`, `NameAlreadyRegistered`.
- O código e a mensagem vêm da TechSpec §8.3, sem variação.
- People, Accounts e Authentication seguem o mesmo formato. Uma regra de negócio violada devolve `BusinessRuleException` (`422`), como `PeopleErrors.DoseOutOfSequence()`, com `vaccination.dose_out_of_sequence`.

## Conversão em HTTP (`Infra/Http`)

Código real em `backend/src/VaccinationCard.Api/Infra/Http/`:

| Peça | O que faz |
|---|---|
| `ApiProblemDetails` | monta o corpo da TechSpec §8.1: `title` pela tabela de status da §8.2, `detail`, `instance`, `code` e `traceId`; `CreateValidation` converte cada campo para o nome do JSON (`Dose.Number` → `dose.number`); `WriteAsync` grava como `application/problem+json`, qualquer que seja o `Accept` |
| `ProblemDetailsExceptionHandler` | `IExceptionHandler`: escolhe o status pelo tipo da exceção; a `ValidationException` do FluentValidation vira `400` `validation.failed`; outra exceção vira `500` `internal.unexpected`, com log `Error` |
| `InvalidModelStateResponse` | `400` `validation.failed` quando o MVC não consegue ler o corpo ou um parâmetro |
| `StatusCodeProblemDetails` | corpo dos erros que o roteamento e o MVC dão sem corpo: `route.not_found`, `route.method_not_allowed` e `request.unsupported_media_type` |

Uma falha que não vem de exceção usa a mesma fábrica. Modelo para a T19, no `OnChallenge` do JwtBearer:

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

Exemplos reais:

- Unit: `backend/tests/VaccinationCard.UnitTests/Modules/Vaccines/Application/UseCases/CreateVaccine/CreateVaccineHandlerTests.cs`. O teste confere o tipo e o código: `Should.ThrowAsync<ConflictException>(act)` e `exception.Code.ShouldBe("vaccine.name_already_registered")`.
- HTTP: `backend/tests/VaccinationCard.IntegrationTests/Modules/Vaccines/Infra/Drivers/Http/VaccinesEndpointTests.cs`. O `ReadProblemAsync` confere o `Content-Type`, e o teste confere o código: `problem.GetProperty("code").GetString().ShouldBe("vaccine.name_already_registered")`.

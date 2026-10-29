---
name: error-handling
description: >-
  Erros: exceções por categoria, códigos estáveis, catálogo por módulo e
  ProblemDetails. Use ao lançar ou criar um erro e ao mudar a resposta de erro da
  API.
---

# Tratamento de erros

Domain e Application lançam uma de quatro exceções de `Shared/Errors`, cada uma com `Code` e `Message`; a Infra converte a exceção em ProblemDetails (`docs/adr/0006-erros-por-categoria-em-problemdetails.md`). Formato, categorias e a lista de códigos: TechSpec §8.

## Erro novo

1. Procure o código na TechSpec §8.3. Se ele não existir, acrescente a linha (código, status e mensagem) na mesma task.
2. Crie o método no catálogo `Modules/<Módulo>/Domain/Errors/<Módulo>Errors.cs` (`references/catalog.md`).
3. Lance com `throw <Módulo>Errors.<Erro>();`.
4. Teste o `Code` da exceção (unit) e o `code` do ProblemDetails (HTTP).
5. O passo termina quando o código existe na TechSpec §8.3, no catálogo e num teste.

## Categoria

| Exceção | Status | Quando |
|---|---|---|
| `NotFoundException` | 404 | o recurso pedido não existe |
| `ConflictException` | 409 | conflito com um dado existente ou com outra operação |
| `BusinessRuleException` | 422 | entrada válida que viola uma RN |
| `UnauthorizedException` | 401 | credencial ausente ou inválida |

Entrada mal formada (campo vazio, fora do limite) é `400` do validator, sem exceção própria. Um corpo que o MVC não consegue ler (JSON malformado, tipo errado, enum desconhecido) também vira `400` `validation.failed`, pelo `InvalidModelStateResponse`.

## Regras

- Código no formato `<entidade>.<motivo>`, em inglês e snake_case. O código publicado é estável: o frontend e os testes dependem dele.
- Mensagem em português, curta e sem dado pessoal (CPF, nome ou e-mail).
- Uma exceção inesperada vira `500` `internal.unexpected`, com os detalhes só no log.
- A conversão para HTTP fica em `Infra/Http`; Domain e Application conhecem só as exceções.
- Toda resposta de erro nasce de `ApiProblemDetails`. Uma falha que não vem de exceção, como o `OnChallenge` do JwtBearer e a recusa do rate limiter, monta o corpo com `ApiProblemDetails.Create` e grava com `ApiProblemDetails.WriteAsync` (`references/catalog.md`).
- Os códigos do próprio pipeline (`validation.failed`, `internal.unexpected`, `route.*` e `request.*`) ficam em `Infra/Http`, na classe que responde com eles.

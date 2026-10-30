---
name: api-endpoint
description: >-
  Rotas HTTP: controllers, respostas, anotações OpenAPI e exemplos no arquivo
  .http. Use ao criar ou mudar uma rota da API.
---

# Rota da API

As rotas, os contratos e as convenções de resposta estão na TechSpec §7. Uma rota nova entra primeiro na tabela da TechSpec §7.2.

## Passos

1. Ache a linha da rota na TechSpec §7.2: método, caminho, autenticação, corpo, sucesso e erros. Rota fora da tabela é lacuna: pare e pergunte ao autor.
2. Escreva o teste HTTP da rota (skill `testing`), cobrindo o sucesso e cada erro da linha.
3. Crie ou edite o controller a partir de `references/controller.md`.
4. Declare um `[ProducesResponseType]` para cada status da linha, mais o `401` nas rotas protegidas.
5. Acrescente o exemplo da rota em `backend/VaccinationCard.http`.
6. O passo termina quando o teste HTTP passa e a rota aparece no Scalar com resumo e todos os status.

## Regras

- O controller monta o command ou a query e chama `ISender.Send`; regra e acesso a dados ficam no caso de uso (A5).
- O controller não devolve resultado de erro (`NotFound()`, `BadRequest()`): o erro sai por exceção do caso de uso (skill `error-handling`). Uma resposta de erro sem corpo recebe nas status code pages um código de rota, como `route.not_found`.
- Criação responde `201` com `CreatedAtAction`, corpo `{ id }` e header `Location`; edição e exclusão, `200` com o `SuccessMessage`; consulta, `200` com o DTO; lista, `200` com o `Paginated<T>`.
- Quando o corpo é igual ao command, o action recebe o command. Quando a rota tem parâmetro, o action recebe um `<Nome>Request` de `Infra/Drivers/Http/Requests` e monta o command.
- Numa lista, o action recebe `page`, `size` e `search` como parâmetros e monta a query. Assim, o OpenAPI e as chaves de `errors` usam os nomes da query string (TechSpec §7.1).
- Parâmetros de rota com restrição de tipo: `{id:guid}`.
- Actions sem o sufixo `Async`: o ASP.NET Core tira o sufixo do nome da action, e o `CreatedAtAction` deixaria de achar a rota.
- Toda rota exige o access token pela fallback policy; `[AllowAnonymous]` só nas rotas anônimas da TechSpec §7.2.
- O CPF trafega só no corpo, nunca na rota ou na query string (RN25).
- `[EndpointSummary]` e `[Tags]` em português.

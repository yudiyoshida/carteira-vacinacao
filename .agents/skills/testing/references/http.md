# Teste HTTP

Uma seção cujo código já existe aponta o arquivo real e lista o que observar nele. Uma seção cujo código ainda não existe traz um modelo; quando a task cria o código, a seção passa a apontar o arquivo real.

Exemplo real: `backend/tests/VaccinationCard.IntegrationTests/Modules/Vaccines/Infra/Drivers/Http/VaccinesEndpointTests.cs`.

O teste herda `HttpTest` e chama a API pelo `Client` da `ApiFactory`, a `WebApplicationFactory<Program>` apontada para o PostgreSQL da fixture. A partir da T14, o relógio dos testes vem de um `FakeTimeProvider`; a partir da T19, o cliente padrão é autenticado (skill `authentication`).

## Montagem

- A URL base fica numa constante: `private const string Url = "/api/v1/vaccines";`.
- O Arrange cria os dados pelo `SendAsync` do caso de uso, herdado de `IntegrationTest`. A rota testada é a única chamada HTTP do teste.
- O corpo da requisição é um objeto anônimo com os nomes do JSON: `new { name = "Hepatite B", primaryDoses = 3, boosterDoses = 0 }`.
- Um método sem atalho no `HttpClient` vai num `HttpRequestMessage` com `new HttpMethod(method)`, enviado por `Client.SendAsync`.

## O que conferir

- Cada status da linha da rota na TechSpec §7.2.
- Na criação: o `201`, o `id` do corpo e o `Location`, com `response.Headers.Location.ShouldNotBeNull().AbsolutePath.ShouldBe($"{Url}/{id}")`.
- Na consulta e na lista: os campos na ordem do contrato da TechSpec §7.3, com `EnumerateObject().Select(field => field.Name)`, e o valor de cada campo.
- Em erro: o `Content-Type` `application/problem+json` e o `code` do corpo. O `ReadProblemAsync` do `HttpTest` confere o `Content-Type` e lê o ProblemDetails como `JsonElement`; o código sai com `GetProperty("code").GetString()`.
- Em `400`: as chaves de `errors` em camelCase, iguais aos campos do JSON. Os parâmetros inválidos de uma lista ficam num `TheoryData`, inclusive o de tipo errado (`page=abc`), que falha no model binding, antes do validator, e responde na mesma chave.
- Em `405` (RN07): o código `route.method_not_allowed`, para cada método que a rota não aceita.
- Nas rotas de autenticação: os atributos do `Set-Cookie`.

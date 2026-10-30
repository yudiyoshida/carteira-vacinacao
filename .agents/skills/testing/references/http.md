# Teste HTTP

Uma seção cujo código já existe aponta o arquivo real e lista o que observar nele. Uma seção cujo código ainda não existe traz um modelo; quando a task cria o código, a seção passa a apontar o arquivo real.

Exemplo real: `backend/tests/VaccinationCard.IntegrationTests/Modules/Vaccines/Infra/Drivers/Http/VaccinesEndpointTests.cs`.

O teste herda `HttpTest` e chama a API pelo `Client` da `ApiFactory`, a `WebApplicationFactory<Program>` apontada para o PostgreSQL da fixture. O `Client` é autenticado: a extensão `CreateAuthenticatedClient`, em `backend/tests/VaccinationCard.IntegrationTests/AuthenticatedClientExtensions.cs`, usa um token do `ITokenService`, sem passar pelo login. Uma factory própria cria o cliente com a mesma extensão, e o `AnonymousClient` do `HttpTest` chama a API sem token, de um IP único por teste (skill `authentication`).

O relógio da API nos testes é um `FakeTimeProvider` parado às 12h UTC de `ApiFactory.Today`, uma data passada: uma data posterior a ela, que a API recusa (RN12), passaria com o relógio real. As datas do Arrange não passam de `ApiFactory.Today`.

## Montagem

- A URL base fica numa constante: `private const string Url = "/api/v1/vaccines";`.
- O Arrange cria os dados pelo `SendAsync` do caso de uso, herdado de `IntegrationTest`. A rota testada é a única chamada HTTP do teste, exceto quando a chamada seguinte faz parte do que se confere, como a consulta depois da exclusão.
- O corpo da requisição é um objeto anônimo com os nomes do JSON: `new { name = "Hepatite B", primaryDoses = 3, boosterDoses = 0 }`.
- Enum vai como texto no objeto anônimo (`type = "Primary"`): o `PostAsJsonAsync` escreveria o enum como número, que a API recusa.
- Um corpo que o objeto anônimo não representa, como um enum desconhecido ou uma data em outro formato, vai como texto, num `StringContent` com `application/json`. Exemplo: `VaccinationsEndpointTests`.
- Uma rota abaixo de outro recurso monta a URL num helper: `private static string Url(Guid personId) => $"/api/v1/people/{personId}/vaccinations";`.
- Um método sem atalho no `HttpClient` vai num `HttpRequestMessage` com `new HttpMethod(method)`, enviado por `Client.SendAsync`.

## O que conferir

- Cada status da linha da rota na TechSpec §7.2.
- Na criação: o `201`, o `id` do corpo e o `Location`, com `response.Headers.Location.ShouldNotBeNull().AbsolutePath.ShouldBe($"{Url}/{id}")`.
- Na consulta e na lista: os campos na ordem do contrato da TechSpec §7.3, com `EnumerateObject().Select(field => field.Name)`, e o valor de cada campo. Um objeto aninhado, como `dose`, tem os campos conferidos do mesmo jeito; o enum sai pelo nome e a data no formato `yyyy-MM-dd`.
- Num contrato com vários níveis, como o cartão, cada nível tem os campos conferidos na ordem da §7.3, e o item de uma lista sai com `EnumerateArray()`. Um campo nulo do contrato continua no JSON e se confere pelo tipo: `GetProperty("nextExpectedDose").ValueKind.ShouldBe(JsonValueKind.Null)`. Exemplo: os testes do cartão em `VaccinationsEndpointTests`.
- Em erro: o `Content-Type` `application/problem+json` e o `code` do corpo. O `ReadProblemAsync` do `HttpTest` confere o `Content-Type` e lê o ProblemDetails como `JsonElement`; o código sai com `GetProperty("code").GetString()`.
- Em `400`: as chaves de `errors` em camelCase, iguais aos campos do JSON. Os parâmetros inválidos de uma lista ficam num `TheoryData`, inclusive o de tipo errado (`page=abc`), que falha no model binding, antes do validator, e responde na mesma chave.
- Em `422`: um `TheoryData` com um caso por regra, cada um violando só a regra do código conferido. O Arrange registra pelo `SendAsync` a dose de que as regras dependem. Exemplo: `VaccinationsEndpointTests`. Quando o mesmo Arrange não permite um caso por regra, a regra vai num teste próprio, como na correção de data: os limites de RN12 numa dose sem vizinhas, e RN14 com duas doses.
- Em `405` (RN07): o código `route.method_not_allowed`, para cada método que a rota não aceita.
- Em `409` de concorrência (RN16): uma factory própria troca o `IPersonRepository` pelo `ConcurrentEditPersonRepository`, que incrementa a versão em outro escopo entre a leitura e a gravação da requisição. Assim, o conflito não depende do tempo de duas requisições reais. O Arrange continua no `SendAsync`, e a rota testada é chamada pelo cliente da factory própria. Exemplo: `VaccinationsEndpointTests`.
- Na edição: o `200` com os campos `["message"]` e a mensagem da TechSpec §7.1, e o valor novo na consulta seguinte. O Act faz as duas chamadas. Exemplo: a correção de data em `VaccinationsEndpointTests`.
- Na exclusão: o `200` com os campos `["message"]` e a mensagem da TechSpec §7.1, e o `404` da consulta seguinte. O Act faz as duas chamadas. Exemplo: `PeopleEndpointTests`. A exclusão de vacinação, em `VaccinationsEndpointTests`, confere também o `422` da dose que não é a última e o cartão sem a vacina depois da única vacinação dela.
- Na lista de pessoas (RN26): o CPF mascarado em cada item e o corpo inteiro, lido com `GetRawText()`, sem o CPF completo, em dígitos ou formatado.
- Em erro sobre dado pessoal (RN25): o corpo inteiro, lido com `problem.GetRawText()`, sem o CPF, em dígitos ou formatado, e sem o nome.
- No log (RN25): o teste sobe uma factory própria com `Fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureLogging(logging => logging.AddFakeLogging()))`, chama a rota pelo cliente dela e lê `factory.Services.GetFakeLogCollector().GetSnapshot()`. Ele confere que o log de comandos do EF Core (`DbLoggerCategory.Database.Command.Name`) foi capturado, para não passar vazio, e que nenhum registro traz o CPF ou o nome. Como o log das duas requisições é o que se confere, o Act faz o cadastro e a repetição do CPF. Exemplo: `PeopleEndpointTests`; o teste falha com o `EnableSensitiveDataLogging` ligado.
- Nas rotas de autenticação: os atributos do `Set-Cookie`, lidos com `SetCookieHeaderValue.Parse`, a ausência do cookie numa recusa e o corpo só com `accessToken`. Duas recusas que não podem se distinguir (RN22) se comparam campo a campo, sem o `traceId`. Exemplo: `Modules/Authentication/Infra/Drivers/Http/AuthEndpointTests.cs`.
- Na renovação e no logout (RN23): o Arrange abre a sessão pelo `SignInCommand`, e o refresh token vai no header `Cookie`, porque o cliente de teste chama por `http` e não envia um cookie `Secure` sozinho. O cookie apagado se confere pelo valor vazio, pelo `Expires` igual a `DateTimeOffset.UnixEpoch` e pelos mesmos atributos do cookie escrito. Um teste que depende da passagem do tempo sobe uma factory com `FakeTimeProvider` próprio, iniciado no mesmo instante da factory compartilhada, e avança o relógio; os tokens dela continuam válidos, porque os segredos são os mesmos. Uma falha inesperada se prova com uma factory própria que troca o `ISessionDao` pelo `FailingSessionDao`: o `500` sai sem `Set-Cookie`. Exemplo: `AuthEndpointTests`.
- Na proteção das rotas (RN21): `Modules/Authentication/Infra/Drivers/Http/ProtectedRoutesTests.cs` lê as rotas do `EndpointDataSource`, então uma rota nova entra na conferência sem mudança no teste. Uma rota anônima nova entra na lista `AnonymousRoutes`. A validade do token se prova com um `FakeTimeProvider` próprio, numa factory própria, avançado depois de o cliente receber o token.
- No limite de tentativas e nos forwarded headers (RN24): `Infra/Http/LoginRateLimitTests.cs`.
  - O `TestServer` não informa o IP do cliente, e o middleware aceita os forwarded headers de um cliente sem IP. Por isso, o cliente nasce de `CreateClientFromIp`, em `backend/tests/VaccinationCard.IntegrationTests/RemoteIpClientExtensions.cs`.
  - Um teste com IP literal sobe uma factory própria, com um limite zerado, porque a factory compartilhada guarda as tentativas entre os testes. O `AnonymousClient` dispensa isso, porque cada teste tem um IP próprio.
  - O `X-Forwarded-For` ignorado se prova com um IP diferente em cada tentativa e o `429` na sexta. O aceito se prova com o `401` de outro cliente pelo mesmo proxy e o `429` do cliente que esgotou o limite.
  - O `X-Forwarded-Proto` se confere pelo esquema do `Location` de uma criação.
  - A biblioteca do limite lê o relógio real, sem `TimeProvider`. O teste fica dentro da janela, e o fim dela não tem teste automatizado.

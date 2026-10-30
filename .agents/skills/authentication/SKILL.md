---
name: authentication
description: >-
  Autenticação: login, refresh token em cookie, logout, JWT, rotas protegidas,
  limite de tentativas e sessão no frontend. Use ao mexer em qualquer parte do
  acesso à API ou à interface.
---

# Autenticação

Desenho: TechSpec §5.3, §5.4, §9 e §11.2 e `docs/adr/0007-refresh-token-em-cookie-httponly.md`. Regras: RN21 a RN24 no PRD.

## Invariantes

Toda mudança preserva estas propriedades, e cada uma tem teste:

- Access token JWT HS256 de 15 minutos, só no header `Authorization`; no frontend, só em memória.
- Refresh token JWT com `jti`, segredo próprio e validade de 7 dias, contados da emissão: cada renovação recomeça os 7 dias (janela deslizante). O banco de dados guarda só o hash SHA-256 dele, na tabela `sessions`.
- Uma sessão por conta: um login novo substitui a sessão anterior, e cada renovação troca o hash (rotação). Um refresh token anterior à rotação é recusado.
- Cookie `refresh_token` com `HttpOnly`, `Secure` (configurável), `SameSite=Strict` e `Path=/api/v1/auth`. O logout e uma renovação recusada apagam o cookie, com os mesmos atributos; uma falha inesperada (`500`) mantém o cookie.
- Credencial inválida recebe sempre `auth.invalid_credentials`, com a mesma mensagem para e-mail inexistente e senha errada. O e-mail inexistente gasta o mesmo tempo, porque o `IPasswordHasher.Verify(null, ...)` confere a senha contra um hash fixo.
- Toda rota nasce protegida pela fallback policy. São anônimos só login, refresh, `/health`, OpenAPI e Scalar. Sem token, uma rota inexistente também responde `401`.
- No máximo 5 tentativas de login por minuto, por IP. Toda requisição à rota de login conta, com credenciais válidas ou não, e a recusa responde `429` `auth.too_many_attempts`. Um IPv4 mapeado em IPv6 conta como o próprio IPv4.
- A API só aceita `X-Forwarded-For` e `X-Forwarded-Proto` de um IP de `ForwardedHeaders:KnownProxies`. Sem proxy configurado, ela ignora os dois headers, porque o middleware aceitaria os headers de qualquer cliente com as listas vazias.
- O hash da senha fica dentro de Accounts: Authentication confere a senha pela `VerifyAccountCredentialsQuery`.
- Os segredos vêm da configuração, com pelo menos 32 bytes e diferentes entre si; a API não sobe fora dessa regra (`JwtOptions.IsValid`). Token, senha e hash ficam fora de logs e de mensagens de erro.
- A validade dos tokens segue o `TimeProvider` da API, na emissão e na validação: a biblioteca lê o relógio real, e o `LifetimeValidator` de `JwtOptions.CreateValidationParameters` a substitui.

## Onde fica

| Peça | Lugar |
|---|---|
| contas, hash de senha e queries de credencial | `Modules/Accounts` |
| `SignIn`, `RefreshSession` e `SignOut` | `Modules/Authentication/Application/UseCases` |
| `ITokenService`, `JwtTokenService` e `JwtOptions` | `Modules/Authentication/Application/Security` e `Modules/Authentication/Infra/Driven/Security` |
| `ISessionDao` e a tabela `sessions` | `Modules/Authentication/Application/Persistence/Dao` e `Modules/Authentication/Infra/Driven/Persistence/EfCore` |
| `RefreshTokenSession` | `Modules/Authentication/Application/Services` |
| cookie do refresh token | `AuthController`; na renovação recusada, a exclusão sai no `OnStarting`, porque o exception handler limpa os headers |
| JwtBearer, `OnChallenge` e fallback policy | `AuthenticationModule.cs` |
| `UseForwardedHeaders`, `UseRateLimiter`, `UseAuthentication` e `UseAuthorization` | `Infra/Http/HttpPipelineExtensions.cs` |
| esquema Bearer no documento e no Scalar | `Infra/OpenApi/OpenApiExtensions.cs` |
| limite de tentativas de login | `Infra/Http/LoginRateLimit.cs`, ligado à rota por `[EnableRateLimiting]` no `AuthController` |
| proxies confiáveis (`TrustOnlyKnownProxies`) | `Infra/Http/HttpPipelineExtensions.cs` |
| corpo do `401` sem token (`OnChallenge`) e do `429` | `ApiProblemDetails`, de `Infra/Http` (skill `error-handling`) |

## Testes

- Os testes HTTP usam um cliente autenticado (`CreateAuthenticatedClient`), com token emitido pelo `ITokenService`, sem passar pelo login; assim o limite de tentativas não interfere.
- O `ProtectedRoutesTests` percorre as rotas da API e confere a lista das rotas anônimas: uma rota anônima nova entra na lista `AnonymousRoutes`.
- Os testes de login, refresh e logout conferem os atributos do `Set-Cookie` (`AuthEndpointTests`).
- Os testes de refresh e logout abrem a sessão pelo `SignInCommand`, sem a rota de login, e enviam o refresh token no header `Cookie`: o cliente de teste chama por `http` e não envia um cookie `Secure` sozinho.
- O `backend/VaccinationCard.http` tem o bloco de login, que guarda o `accessToken`, e cada bloco de rota protegida leva `Authorization: Bearer {{accessToken}}`.
- Expiração e rotação se testam com `FakeTimeProvider`, avançado depois da emissão do token (`JwtTokenServiceTests` e `ProtectedRoutesTests`).
- O limite de tentativas e os forwarded headers se testam em `Infra/Http/LoginRateLimitTests.cs`, com o IP remoto definido por `CreateClientFromIp`. A biblioteca do limite lê o relógio real, então o fim da janela fica sem teste automatizado (`testing/references/http.md`).

## Frontend

A sessão no navegador segue `references/frontend-session.md`: access token num signal do `AuthSessionService`, `hydrate()` na inicialização e `authInterceptor` com um único refresh compartilhado.

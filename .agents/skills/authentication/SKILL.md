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
- Refresh token JWT com `jti`, segredo próprio e validade de 7 dias. O banco de dados guarda só o hash SHA-256 dele, na tabela `sessions`.
- Uma sessão por conta: um login novo substitui a sessão anterior, e cada renovação troca o hash (rotação). Um refresh token anterior à rotação é recusado.
- Cookie `refresh_token` com `HttpOnly`, `Secure` (configurável), `SameSite=Strict` e `Path=/api/v1/auth`.
- Credencial inválida recebe sempre `auth.invalid_credentials`, com a mesma mensagem para e-mail inexistente e senha errada.
- Toda rota nasce protegida pela fallback policy. São anônimos só login, refresh, `/health`, OpenAPI e Scalar.
- O hash da senha fica dentro de Accounts: Authentication confere a senha pela `VerifyAccountCredentialsQuery`.
- Os segredos vêm da configuração, com pelo menos 32 bytes. Token, senha e hash ficam fora de logs e de mensagens de erro.

## Onde fica

| Peça | Lugar |
|---|---|
| contas, hash de senha e queries de credencial | `Modules/Accounts` |
| `SignIn`, `RefreshSession` e `SignOut` | `Modules/Authentication/Application/UseCases` |
| `ITokenService` e a implementação JWT | `Modules/Authentication/Application/Security` e `Modules/Authentication/Infra/Driven/Security` |
| `ISessionDao` e a tabela `sessions` | `Modules/Authentication/Application/Persistence/Dao` e `Modules/Authentication/Infra/Driven/Persistence/EfCore` |
| `RefreshTokenSession` | `Modules/Authentication/Application/Services` |
| cookie do refresh token | `AuthController` |
| JwtBearer e fallback policy | `AuthenticationModule.cs` |
| limite de tentativas de login | `Infra/Http` |

## Testes

- Os testes HTTP usam um cliente autenticado, com token emitido pelo `ITokenService`, sem passar pelo login; assim o limite de tentativas não interfere.
- Os testes de login, refresh e logout conferem os atributos do `Set-Cookie`.
- Expiração e rotação se testam com `FakeTimeProvider` ou com tokens emitidos já vencidos.

## Frontend

A sessão no navegador segue `references/frontend-session.md`: access token num signal, refresh na inicialização e `authInterceptor` com um único refresh compartilhado.

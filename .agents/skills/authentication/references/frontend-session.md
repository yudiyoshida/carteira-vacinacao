# Sessão no frontend

Exemplos reais:

- chamadas da API: `frontend/src/app/core/api/auth-api.service.ts`;
- access token e refresh compartilhado: `frontend/src/app/core/auth/auth-session.service.ts`;
- header e renovação: `frontend/src/app/core/interceptors/auth.interceptor.ts`;
- proteção das rotas: `frontend/src/app/core/guards/auth.guard.ts`, ligado em `frontend/src/app/modules/main/main.routes.ts`;
- inicialização: `frontend/src/app/app.config.ts`;
- login: `frontend/src/app/modules/auth/pages/login/login.ts`;
- "Sair": `frontend/src/app/modules/main/main.ts`.

Os testes seguem a seção "Sessão" de `testing/references/frontend.md`.

## Regras

- O access token fica só num signal do `AuthSessionService`; `localStorage` e `sessionStorage` ficam sem token.
- O navegador envia o cookie do refresh token sozinho, porque interface e API estão na mesma origem. O código nunca lê esse cookie.
- O `hydrate()` roda num `provideAppInitializer` e termina antes da primeira navegação. Sem cookie válido, a sessão começa vazia, e o `authGuard` leva o operador ao `/login`.
- Um `401` do login ou do refresh encerra o fluxo, sem disparar outro refresh.
- Requisições que recebem `401` ao mesmo tempo compartilham um único refresh (`shareReplay`). Quando o `401` chega depois do fim do refresh, o token atual já é outro: a requisição repete com ele, sem outro refresh.
- No interceptor, o `catchError` do refresh fica antes do `switchMap`: ele trata só a falha do refresh, e o erro da requisição repetida segue para quem chamou, sem laço.
- Um refresh que falha limpa a sessão e navega para `/login`. Quem chamou recebe o erro do refresh, e o toast mostra "Sessão expirada. Entre novamente.".
- O logout é uma rota protegida comum: com o access token vencido, o interceptor renova a sessão e repete o logout. Com sucesso, a sessão é limpa, o `message` vai para o toast e o operador volta ao login. Com falha, a sessão continua e o erro vai para o toast.

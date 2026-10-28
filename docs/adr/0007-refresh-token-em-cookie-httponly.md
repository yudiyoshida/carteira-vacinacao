# Refresh token em cookie HttpOnly, com rotação e uma sessão por conta

O access token (JWT de 15 minutos) vai no header `Authorization` e fica só em memória no frontend. O refresh token (JWT de 7 dias, com segredo próprio e `jti`) vai num cookie `HttpOnly`, `Secure` e `SameSite=Strict`, restrito a `/api/v1/auth`. O servidor guarda só o hash SHA-256 do refresh token, uma sessão por conta, e troca o par a cada renovação. Como o JavaScript não lê o refresh token, um XSS não rouba a sessão; e como o nginx serve interface e API na mesma origem, não há CORS.

## Opções consideradas

- **Tokens no corpo JSON, com o refresh token no `localStorage`:** mais simples, mas o token fica exposto a XSS.
- **Só um access token de 60 minutos, sem refresh token:** mais simples, mas obriga um novo login a cada hora.

## Consequências

- Um novo login encerra a sessão anterior da mesma conta.
- Um navegador que recusa cookie `Secure` em `http://localhost` precisa de `RefreshCookie:Secure=false`, só em desenvolvimento.

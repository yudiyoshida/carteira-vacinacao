---
name: angular-feature
description: >-
  Frontend Angular: features, componentes, services, formulários, diálogos e
  rotas. Use ao criar ou mudar uma tela, um formulário ou uma chamada da API no
  frontend.
---

# Feature no Angular

Estrutura e regras: seção "Frontend" de `.agents/rules/architecture.md`. Telas, sessão e testes: TechSpec §11 e PRD §7.

## Passos

1. Gere os arquivos pelo CLI, a partir da raiz do repositório: `npm --prefix frontend run ng -- generate component features/<feature>/<nome>`.
2. Crie o service da feature e o teste dele com `HttpTestingController` (`references/feature.md`).
3. Monte o componente: estado em signals, HTML com `@if` e `@for` e componentes do Angular Material.
4. Num formulário, um `400` da API leva os `errors` do ProblemDetails aos campos, pelo tratamento de `core/http`.
5. Registre a rota lazy em `features/<feature>/<feature>.routes.ts` e ligue-a em `app.routes.ts`, dentro do shell protegido.
6. O passo termina quando `npm --prefix frontend run build` e `npm --prefix frontend test` passam e a tela funciona contra a API local.

## Regras

- Textos da interface em português, com datas `dd/MM/yyyy` na tela e `yyyy-MM-dd` na API.
- Componente com `ChangeDetectionStrategy.OnPush`, dependências por `inject()` e entradas e saídas por `input()` e `output()`.
- A mensagem de sucesso da API (`message`) e o erro sem campo aparecem no snackbar.
- Ação destrutiva passa pelo `ConfirmDialog`.
- O rótulo de dose sai sempre do `DoseLabelPipe` (RN18).
- O frontend decide pelo `code` do ProblemDetails, nunca pelo texto da mensagem.

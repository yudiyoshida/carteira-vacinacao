---
name: angular-feature
description: >-
  Frontend Angular: páginas, componentes, services da API, formulários, modais e
  rotas. Use ao criar ou mudar uma tela, um formulário ou uma chamada da API no
  frontend.
---

# Feature no Angular

Estrutura, nomes e regras: seção "Frontend" de `.agents/rules/architecture.md`. Telas, sessão, tema e testes: TechSpec §11 e PRD §7.

## Passos

1. **Contrato:** tipos do corpo e da resposta em `core/models/<recurso>.model.ts`, iguais aos da TechSpec §7.3.
2. **Service:** métodos da API em `core/api/<recurso>-api.service.ts`, com teste por `HttpTestingController` (`references/feature.md`).
3. **Página:** gere pelo CLI, a partir da raiz do repositório: `npm --prefix frontend run ng -- generate component modules/main/pages/<página>`. Um componente usado só pela página fica em `pages/<página>/components/`.
4. **Tela:** estado em signals; lista com `data-table`, `page-header`, `empty-state` e `skeleton`; formulário com os campos de `shared/components/inputs` e o `submit-button`.
5. **Erros e avisos:** um `400` leva os `errors` aos campos pelo `core/utils/api-error.ts`; os outros erros e a mensagem de sucesso vão para o `ToastService`; ação destrutiva passa pelo `confirm-modal`.
6. **Rota:** segmento novo no `RoutesEnum` e rota lazy em `modules/<módulo>/<módulo>.routes.ts`.
7. O passo termina quando `npm --prefix frontend run lint`, `npm --prefix frontend run build` e `npm --prefix frontend test` passam e a tela funciona contra a API local, nos temas claro e escuro.

## Regras

- Textos da interface em português.
- O rótulo de dose sai sempre do `DoseLabelPipe` (RN18).
- O frontend decide pelo `code` do ProblemDetails, nunca pelo texto da mensagem.
- Um componente vai para `shared/` quando uma segunda página passa a usá-lo; até lá, ele fica na página.

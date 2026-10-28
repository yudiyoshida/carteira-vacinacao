---
name: new-feature
description: >-
  Fatia vertical: ordem de trabalho do domínio à tela e a skill de cada passo. Use
  ao executar uma task de docs/TASKS.md ou ao começar uma funcionalidade nova.
---

# Fatia vertical

O início e a entrega de uma task seguem `.agents/context.md`. Esta skill ordena o meio: o que construir primeiro e qual skill usar em cada passo. Pule os passos que a task não toca.

1. **Especificação:** a entrada da task, as RN do PRD e as seções da TechSpec definem o que entra. Uma lacuna para a task: pergunte ao autor.
2. **Domínio** (`domain-modeling`): as regras, em TDD.
3. **Caso de uso** (`use-case`): testes do validator e do handler, depois os tipos.
4. **Persistência** (`persistence`): registro, configuração, migration, DAO ou Repository.
5. **Erros** (`error-handling`): códigos novos na TechSpec §8.3 e no catálogo do módulo.
6. **Rota** (`api-endpoint`): teste HTTP, controller, OpenAPI e exemplo no `.http`.
7. **Acesso** (`authentication`): quando a task mexe em login, sessão ou proteção de rota.
8. **Tela** (`angular-feature`): service, componentes, rota e testes do frontend.
9. **Cobertura** (`testing`): cada caso de uso da task com a matriz da TechSpec §13.2 completa.
10. **Revisão** (`task-review`): o diff contra a especificação e os padrões.

A fatia termina quando a definição de pronto (`docs/TASKS.md`, seção 2) fecha e a revisão não deixa achado bloqueante.

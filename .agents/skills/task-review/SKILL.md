---
name: task-review
description: >-
  Revisão de task: confere o diff contra a especificação e os padrões do
  repositório e lista os achados por gravidade. Use antes de entregar uma task,
  ao revisar um diff e na revisão final do repositório.
---

# Revisão de task

A revisão compara o que mudou com duas fontes: a especificação (a entrada da task, o PRD e a TechSpec) e os padrões (`.agents/rules/architecture.md` e as skills de cada área).

## Passos

1. **Escopo:** `git status` e `git diff` mostram o que mudou, inclusive os arquivos novos. Na revisão final do repositório, o escopo é o código inteiro.
2. **Especificação:** cada item de "Entrega" e de "Verificação" da task tem código ou teste correspondente, e o diff fica dentro da task.
3. **Padrões:** aplique cada seção de `references/checklist.md` ao diff.
4. **Comandos:** rode o build e os testes e anote o resultado.
5. **Relatório:** um achado por linha, do mais grave para o menos grave, no formato `<arquivo>:<linha> — <gravidade> — <problema>. <correção>.` As gravidades são:
   - **bloqueante:** quebra uma RN, um contrato da TechSpec, a segurança ou o build;
   - **importante:** desvia de um padrão ou deixa uma regra sem teste;
   - **menor:** legibilidade e nomes.
6. A revisão termina quando cada seção do checklist foi aplicada e cada achado tem arquivo, linha e correção.

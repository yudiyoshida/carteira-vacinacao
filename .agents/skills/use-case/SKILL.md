---
name: use-case
description: >-
  Casos de uso com MediatR: command ou query, handler, validator e output. Use ao
  criar ou mudar um caso de uso, uma validação de entrada ou uma consulta a outro
  módulo.
---

# Caso de uso

Um caso de uso é um command (muda estado) ou uma query (só lê) do MediatR 12.5, numa pasta própria em `Modules/<Módulo>/Application/UseCases/<Verbo><Entidade>/`. O fluxo da requisição está na TechSpec §4.4, e os casos de uso de cada módulo, na TechSpec §5.

## Passos

1. **Nome:** `<Verbo><Entidade>`, com os verbos de `.agents/rules/architecture.md`. A pasta e os tipos levam o nome: `<Nome>Command` ou `<Nome>Query`, `<Nome>Handler`, `<Nome>Validator` e `<Nome>Output`. A saída pode ser também um DTO de `Application/Dtos` ou um `SuccessMessage`.
2. **Testes primeiro:** teste unitário do validator e do handler, com os ports mockados (skill `testing`).
3. **Tipos:** crie os arquivos a partir de `references/examples.md`. O `AddCore` (`Core/CoreExtensions.cs`) registra o handler e o validator pela varredura do assembly, então o caso de uso não precisa de registro.
4. **Integração:** teste do handler com DI real e banco de dados.
5. O passo termina quando os testes unitários e de integração passam e, se o caso de uso tiver rota, a rota existe (skill `api-endpoint`).

## Handler

- Orquestra: lê pelos ports ou por query de outro módulo, chama o domínio (ou o DAO, num módulo CRUD), grava e devolve a saída.
- Devolve output, DTO ou `SuccessMessage`; a entidade fica dentro do handler.
- Aplica `Trim` nos nomes antes de chamar o domínio; a senha passa sem alteração.
- Lê "hoje" pelo `IClock` (`Core/Time`) e passa a data ao domínio.
- Um recurso inexistente lança o `NotFound` do catálogo do módulo (skill `error-handling`).
- Outro módulo entra só por query: `await sender.Send(new FindVaccineByIdQuery(id), cancellationToken)` (`docs/adr/0011-comunicacao-entre-modulos-por-queries.md`).
- O `CancellationToken` segue para toda chamada assíncrona.

## Validator

- Valida formato e limites da entrada. A regra que depende de estado (CPF repetido, sequência de doses) fica no handler ou no domínio.
- Mensagens em português, uma por regra de campo, no estilo do exemplo da TechSpec §8.1.
- Paginação e busca reutilizam as regras de `Core/Validation` (`ValidPage`, `ValidSize` e `ValidSearch`).
- O `ValidationBehavior` roda os validators antes do handler e responde `400`; o handler recebe só entrada válida.

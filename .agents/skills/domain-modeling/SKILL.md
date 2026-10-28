---
name: domain-modeling
description: >-
  Modelagem de domínio DDD-light: agregado Person, value objects, factories e
  termos do glossário. Use ao criar ou mudar uma regra de negócio, um value object
  ou um termo do domínio.
---

# Modelagem de domínio

DDD só onde há regra (`docs/adr/0002-ddd-light-so-onde-ha-regra.md`). O agregado `Person`, de People, concentra as regras de vacinação; Vaccines tem só value objects e enums; Accounts e Authentication não têm entidade. As regras (RN) estão no PRD §6, e o desenho do agregado, na TechSpec §5.2.

## Regra nova ou alterada

1. Ache a RN no PRD. Uma regra sem RN é lacuna: pare e pergunte ao autor.
2. Escreva o teste da regra, com `[Trait("RN", "RNxx")]`, e veja falhar (skill `testing`).
3. Implemente no dono da regra: a raiz do agregado para regra entre vacinações, o value object para regra do próprio valor.
4. A violação lança o erro do catálogo do módulo (skill `error-handling`).
5. O passo termina quando o teste novo e os testes das outras RN do mesmo método passam.

## Modelos

- Value object, entidade e factory: `references/building-blocks.md`.
- Agregado `Person` e os casos de teste de cada regra: `references/person-aggregate.md`.

## Regras de modelagem

- O domínio recebe "hoje" como parâmetro (`DateOnly today`). O handler lê o relógio pelo `IClock`.
- A entidade nasce pela factory: `Create` valida e gera o id com `Guid.CreateVersion7()`; `Load` reconstrói a entidade lida do banco de dados, sem validar, e só o Repository chama `Load`.
- Os construtores da entidade são `internal`, e só a factory os chama. A lista de filhos é privada e sai como `IReadOnlyList<T>`.
- Value object é um `sealed record` imutável, que se valida ao nascer.
- Entrada inválida do operador é barrada antes, pelo validator (`400`). A guarda do domínio protege a invariante contra erro de programação, com `ArgumentException`.
- Violação de regra de negócio lança a exceção da categoria (`404`, `409` ou `422`), pelo catálogo do módulo.

## Glossário

Um termo novo do domínio entra no `GLOSSARY.md` na mesma task, no formato das entradas existentes: termo em português, nome no código entre parênteses, definição de uma frase e a lista "Evitar".

---
name: testing
description: >-
  Testes: unit, integração com PostgreSQL, HTTP, arquitetura e frontend com
  Vitest. Use ao escrever ou mudar um teste, no ciclo TDD de uma regra e ao
  investigar um teste falhando.
---

# Testes

O que testar em cada camada está na TechSpec §13. Cada RN tem pelo menos um teste automatizado (PRD §11).

## Ciclo TDD de uma regra

1. Escreva o teste da regra, com `[Trait("RN", "RNxx")]`.
2. Rode o teste e veja vermelho pelo motivo esperado: asserção falhando ou tipo ainda inexistente.
3. Implemente o mínimo para o verde.
4. Refatore com os testes verdes.
5. O ciclo termina quando o teste novo e os vizinhos passam.

## Onde fica cada teste

| Alvo | Projeto | Modelo |
|---|---|---|
| entidade, value object, factory, validator e handler com ports mockados | `VaccinationCard.UnitTests` | `references/unit.md` |
| handler com DI real, DAO e Repository | `VaccinationCard.IntegrationTests` | `references/integration.md` |
| rota HTTP | `VaccinationCard.IntegrationTests` | `references/http.md` |
| regras A1–A7 | `VaccinationCard.ArchitectureTests` | `references/architecture.md` |
| frontend | `frontend/src`, ao lado do código | `references/frontend.md` |

No backend, o arquivo de teste espelha a pasta do código: `backend/tests/VaccinationCard.UnitTests/Modules/People/Domain/Entities/PersonTests.cs`.

## Convenções

- Nome `Should_<Resultado>_When_<Condição>`, em inglês. No teste de arquitetura, o nome começa pelo id da regra (`A2_...`).
- `_sut` guarda o objeto testado.
- `Make<Verbo><Entidade>Input()` devolve uma entrada válida, e o teste muda só o campo do caso, com `with`.
- Comentários `// Arrange`, `// Act` e `// Assert` em cada teste.
- Asserções com Shouldly e mocks com NSubstitute. FluentAssertions fica fora do projeto, porque a v8 tem licença comercial.
- Um erro se confere pelo `Code` da exceção ou pelo `code` do ProblemDetails, nunca pelo texto da mensagem.
- O tempo vem de `FakeTimeProvider`; nenhum teste depende do relógio real.
- Toda chamada assíncrona recebe `TestContext.Current.CancellationToken`: o analisador do xUnit v3 avisa quando falta, e warning quebra o build.
- No frontend, o teste de uma regra leva o código da RN no nome: `it('RN18: ...')`.

## Comandos

```bash
dotnet test backend/VaccinationCard.slnx --filter "RN=RN10"   # testes de uma regra
dotnet test backend/tests/VaccinationCard.UnitTests           # só os unitários
npm --prefix frontend test
```

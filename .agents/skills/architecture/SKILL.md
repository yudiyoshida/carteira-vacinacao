---
name: architecture
description: >-
  Arquitetura do backend: onde cada arquivo fica e o que cada camada pode usar.
  Use ao criar um módulo ou uma pasta, ao decidir onde fica um tipo novo e quando
  um teste de arquitetura falhar.
---

# Arquitetura

As regras estão em `.agents/rules/architecture.md`: os módulos, a tabela "Onde cada coisa fica", as regras A1–A7 e as convenções de código. Leia esse arquivo antes dos passos abaixo. O porquê está na TechSpec §4 e em `docs/adr/0001-projeto-unico-com-modulos.md`.

## Tipo novo

1. Ache o artefato na tabela "Onde cada coisa fica" e crie o arquivo na pasta indicada, com o namespace igual à pasta.
2. Confira cada `using` contra A1–A7. Uma dependência proibida mostra que o tipo está na camada errada: mova o tipo ou troque a dependência por um port.
3. O passo termina quando `dotnet test backend/tests/VaccinationCard.ArchitectureTests` passa.

## Módulo novo

1. Crie as pastas de `references/module-skeleton.md` que o módulo usa.
2. Crie `<Módulo>Module.cs`, com `Add<Módulo>Module` registrando os ports e adapters do módulo, e chame esse método no `Program.cs`.
3. Acrescente o módulo à tabela "Módulos" de `.agents/rules/architecture.md` e à TechSpec §5.
4. O passo termina quando o build e os testes de arquitetura passam.

## Teste de arquitetura falhando

1. Leia a mensagem: ela traz o tipo e a dependência que violam a regra.
2. Corrija o código: mova o tipo, use um port da Application ou consulte o outro módulo por query (A4).
3. A regra muda só por decisão do autor. Nesse caso, `.agents/rules/architecture.md`, a TechSpec §4.2 e o teste mudam na mesma task.

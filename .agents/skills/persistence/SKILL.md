---
name: persistence
description: >-
  Persistência com EF Core e PostgreSQL: registros, configurações, DAO, Repository,
  migrations e seeds. Use ao criar ou mudar uma tabela, uma consulta, a gravação
  do agregado ou os dados de demonstração.
---

# Persistência

A leitura passa por DAO, e a escrita do agregado, por Repository (`docs/adr/0003-dao-para-leitura-e-repository-para-escrita.md`). O EF Core mapeia registros de persistência (`*Record`), separados das entidades (`docs/adr/0004-modelos-de-persistencia-separados.md`). O esquema, os índices e a concorrência estão na TechSpec §6.

## Tabela nova ou alterada

1. Crie ou edite o registro e a configuração em `Modules/<Módulo>/Infra/Driven/Persistence/EfCore/` (`references/records.md`).
2. Gere a migration:

   ```bash
   dotnet tool restore
   dotnet ef migrations add <Nome> --project backend/src/VaccinationCard.Api --output-dir Infra/Database/Migrations
   ```

   O nome descreve o efeito, em PascalCase: `CreateVaccines`, `AddPersonVersion`.
3. Leia a migration gerada e confira tabela, colunas, índices e checks com a TechSpec §6.1.
4. O passo termina quando a TechSpec §6.1 descreve a tabela como ela ficou e os testes de integração passam.

## Leitura (DAO)

Port em `Application/Persistence/Dao`, implementação na Infra do módulo. Modelo com projeção, paginação, busca e gravação de módulo CRUD: `references/dao.md`.

## Escrita do agregado (Repository)

Port em `Application/Persistence/Repository`, implementação na Infra do módulo. Modelo com leitura rastreada e diferença das vacinações: `references/repository.md`.

## Regras

- Os ports recebem e devolvem DTOs, entidades e tipos primitivos. `IQueryable`, `DbSet`, `Expression` e qualquer tipo do EF Core ficam na Infra.
- `ToTable("<tabela>")` em toda configuração, com o nome da TechSpec §6.1: a classe termina em `Record`, e a convenção snake_case geraria `vaccine_record`.
- Chave `Guid` com `ValueGeneratedNever()`: o id vem de `Guid.CreateVersion7()`, no domínio ou no handler.
- Índice único com nome explícito (`ux_<tabela>_<colunas>`). A tradução do `23505` usa esse nome para escolher o código de erro.
- `created_at` com default `now()` no banco de dados.
- DAO com `AsNoTracking()` e projeção direta no DTO, com `Select`.
- Busca por nome com `unaccent` e `ILIKE`, com o termo escapado (`references/dao.md`).
- Seeds em `Infra/Database/Seeds`: cada seed roda só com a tabela vazia, e a pessoa de demonstração nasce pelo agregado, para respeitar as regras.

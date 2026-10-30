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
3. Leia a migration gerada e confira tabela, colunas, índices e checks com a TechSpec §6.1. A migration fica como o EF Core gera: o `.editorconfig` trata `Migrations/` como código gerado, então o namespace com bloco não quebra o build.
4. O passo termina quando a TechSpec §6.1 descreve a tabela como ela ficou e os testes de integração passam.

## Leitura (DAO)

Port em `Application/Persistence/Dao`, implementação na Infra do módulo. O exemplo real (`VaccineDao`) e o que observar nele, com projeção, paginação, busca e gravação de módulo CRUD: `references/dao.md`.

## Escrita do agregado (Repository)

Port em `Application/Persistence/Repository`, implementação na Infra do módulo. Modelo com leitura rastreada e diferença das vacinações: `references/repository.md`.

## Regras

- Os ports recebem e devolvem DTOs, entidades, value objects e tipos primitivos. `IQueryable`, `DbSet`, `Expression` e qualquer tipo do EF Core ficam na Infra.
- `ToTable("<tabela>")` em toda configuração, com o nome da TechSpec §6.1: a classe termina em `Record`, e a convenção snake_case geraria `vaccine_record`.
- Chave `Guid` com `ValueGeneratedNever()`: o id vem de `Guid.CreateVersion7()`, no domínio ou no handler.
- Índice único com nome explícito (`ux_<tabela>_<colunas>`), numa constante pública da configuração. O `HasDatabaseName` e a tradução do `23505` usam a mesma constante, e a tradução escolhe o código de erro por ela.
- Check constraint com nome explícito (`ck_<tabela>_<coluna>`) e os mesmos limites do value object e do validator.
- Chave estrangeira entre módulos no `AppDbContext`, porque a configuração de um módulo não referencia o registro de outro (A3, `references/records.md`).
- `created_at` com default `now()` no banco de dados.
- Um valor normalizado para a unicidade (`normalized_name`) é calculado no adaptador, com a mesma função na gravação e na consulta.
- DAO com `AsNoTracking()` e projeção direta no DTO, com `Select`. A lista ordena por nome, na collation do banco de dados (TechSpec §6.2).
- Busca por nome com `unaccent` e `ILIKE`, com o termo escapado (`references/dao.md`).
- Seeds em `Infra/Database/Seeds` (`AccountSeed`, `VaccineSeed` e `PersonSeed`), ligados por `Database:SeedOnStartup`: cada seed roda só com a tabela vazia, dentro da transação do `SeedDatabaseAsync` (`Infra/Database/DatabaseExtensions.cs`). Os dados entram pelos casos de uso, e a pessoa de demonstração nasce pelo agregado, para respeitar as regras; só a conta, que não tem caso de uso de criação, grava pelo DAO. Um dado que depende de algo que o banco de dados não traz fica de fora, com um aviso no log, sem impedir a API de subir, como a pessoa sem as vacinas do cartão (`PersonSeed`). Regras na TechSpec §6.4.

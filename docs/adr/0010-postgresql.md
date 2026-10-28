# PostgreSQL como banco de dados

O banco de dados é o PostgreSQL 18, num container do Docker Compose e também nos testes de integração (Testcontainers). Ele roda igual em qualquer máquina com Docker, inclusive Apple Silicon, e tem a extensão `unaccent` para a busca por nome sem acentos.

## Opções consideradas

- **SQLite:** zero setup, mas com um motor diferente do real e limitações de migration no EF Core.
- **SQL Server:** comum no mercado, mas com imagem pesada e setup lento para quem roda o projeto.

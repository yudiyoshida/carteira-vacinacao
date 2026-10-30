# Teste de integração

Uma seção cujo código já existe aponta o arquivo real e lista o que observar nele. Uma seção cujo código ainda não existe traz um modelo; quando a task cria o código, a seção passa a apontar o arquivo real.

## Infraestrutura

Código em `backend/tests/VaccinationCard.IntegrationTests/`, na raiz do projeto.

- `IntegrationFixture`, fixture de assembly do xUnit v3: um container `postgres:18` por execução e uma `ApiFactory` apontada para ele. A subida da API aplica as migrations (`Database:MigrateOnStartup=true`), uma vez por execução.
- `ApiFactory` (`WebApplicationFactory<Program>`) roda no ambiente `Testing`: o `appsettings.Development.json` fica de fora, e as chaves vêm de `UseSetting`. Um teste troca uma chave com `fixture.Factory.WithWebHostBuilder(builder => builder.UseSetting(...))` e descarta a factory nova no fim.
- `IntegrationTest` limpa o banco de dados antes de cada teste com o Respawn, que preserva `__EFMigrationsHistory`; `HttpTest` acrescenta o `Client`.
- Um teste que não lê nem grava dados (`/health`, OpenAPI, migrations) recebe a `IntegrationFixture` no construtor, sem base e sem limpeza. Exemplo: `Infra/Database/HealthCheckTests.cs`.
- O Respawn recusa um banco sem tabelas, então o `Respawner` nasce na primeira limpeza.
- O projeto roda os testes em sequência (`[assembly: Parallelization(Mode = ParallelMode.None)]`), porque todos compartilham o mesmo banco de dados.
- O Docker precisa estar rodando.

## Handler com DI real

Exemplos reais: `backend/tests/VaccinationCard.IntegrationTests/Modules/Vaccines/Application/UseCases/`, nas pastas `CreateVaccine`, `FindAllVaccines` e `FindVaccineById`.

- A classe herda a base pelo construtor primário: `CreateVaccineHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)`.
- A base `IntegrationTest` limpa o banco de dados antes de cada teste e oferece dois helpers:
  - `SendAsync`: envia o command ou a query num escopo de DI novo, como uma requisição HTTP, passando pelos behaviors do MediatR. O Arrange também prepara os dados por ele.
  - `ExecuteDbContextAsync`: entrega um `AppDbContext` num escopo próprio, para conferir o que foi gravado: `ExecuteDbContextAsync(db => db.Set<VaccineRecord>().SingleAsync(x => x.Id == output.Id, cancellationToken))`.
- O teste de integração prova o que o mock não prova: a gravação real, a regra que depende do banco de dados (o nome repetido, num `Theory`) e o `NotFound` com o DAO real.
- Um erro se confere pelo `Code` da exceção, como no unit.

## Pipeline HTTP

Código em `backend/tests/VaccinationCard.IntegrationTests/Infra/Http/`. A base `PipelineTest` sobe a API com as rotas do `PipelineTestController`, que existe só nos testes e entra por `AddApplicationPart`, e com `AddFakeLogging`. O teste herda `PipelineTest` e usa `Client` e `Logs` (o `FakeLogCollector`), sem limpeza do banco de dados. Uma rota nova no controller de teste provoca um erro que nenhum módulo provoca ainda. Exemplos: `SecurityHeadersTests`, `RequestLoggingTests` e `ErrorResponseTests`.

O `GetAsync` do `HttpClient` lê o corpo inteiro antes de devolver a resposta. Assim, o log da requisição já está no `Logs` quando o teste confere.

## DAO e Repository

Exemplo real: `backend/tests/VaccinationCard.IntegrationTests/Modules/Vaccines/Infra/Driven/Persistence/EfCore/VaccineDaoTests.cs`.

- O adaptador roda contra o banco de dados real: projeção, ordem, paginação, busca sem acento e tradução do `23505`.
- A classe tem construtor clássico: cria um escopo com `fixture.Factory.Services.CreateAsyncScope()` e o `_sut` com o `AppDbContext` desse escopo. O `DisposeAsync` descarta o escopo e chama a base.
- O Arrange grava pelo próprio adaptador (`_sut.SaveAsync`), e a conferência lê pelo `ExecuteDbContextAsync`, fora do escopo que gravou.
- Uma check constraint se prova com um valor fora do limite gravado pelo adaptador. A `DbUpdateException` traz uma `PostgresException` com `PostgresErrorCodes.CheckViolation` e o `ConstraintName` da configuração.
- A ordem se prova com uma lista fora de ordem, como as vacinas do PRD §9 em `UnorderedNames`. A busca se prova com maiúsculas, acentos e os curingas `%`, `_` e `\`.
- Num DAO só de leitura, o Arrange cadastra pelo caso de uso (`SendAsync`): `backend/tests/VaccinationCard.IntegrationTests/Modules/People/Infra/Driven/Persistence/EfCore/PersonDaoTests.cs`.
- Repository: `backend/tests/VaccinationCard.IntegrationTests/Modules/People/Infra/Driven/Persistence/EfCore/PersonRepositoryTests.cs`. O teste da leitura cadastra a pessoa em outro escopo (`SendAsync`), para a consulta ir ao banco de dados, e não ao registro que o `DbContext` do `_sut` já rastreia.

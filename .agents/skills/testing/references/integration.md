# Teste de integração

Uma seção cujo código já existe aponta o arquivo real e lista o que observar nele. Uma seção cujo código ainda não existe traz um modelo; quando a task cria o código, a seção passa a apontar o arquivo real.

## Infraestrutura

Código em `backend/tests/VaccinationCard.IntegrationTests/`, na raiz do projeto.

- `IntegrationFixture`, fixture de assembly do xUnit v3: um container `postgres:18` por execução e uma `ApiFactory` apontada para ele. A subida da API aplica as migrations (`Database:MigrateOnStartup=true`), uma vez por execução.
- `ApiFactory` (`WebApplicationFactory<Program>`) roda no ambiente `Testing`: o `appsettings.Development.json` fica de fora, e as chaves vêm de `UseSetting`. Um teste troca uma chave com `fixture.Factory.WithWebHostBuilder(builder => builder.UseSetting(...))` e descarta a factory nova no fim.
- `IntegrationTest` limpa o banco de dados antes de cada teste com o Respawn, que preserva `__EFMigrationsHistory`; `HttpTest` acrescenta o `Client`, autenticado, e o `AnonymousClient`.
- Uma conta não tem caso de uso que a crie: o Arrange usa o `CreateAccountAsync` do `IntegrationTest`, que grava pelo `IAccountDao`, como o seed.
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
- Num caso de uso sobre o agregado, o teste prova também o estado lido do banco de dados entre requisições, como a dose repetida (RN11), a query do outro módulo (`vaccine.not_found`) e o relógio da `ApiFactory` (RN12, com `ApiFactory.Today`). O Arrange cria a pessoa e a vacina pelo `SendAsync` num helper `Make<Verbo><Entidade>InputAsync`. Exemplo: `Modules/People/Application/UseCases/RegisterVaccination/RegisterVaccinationHandlerTests.cs`.
- Um erro se confere pelo `Code` da exceção, como no unit.

## Pipeline HTTP

Código em `backend/tests/VaccinationCard.IntegrationTests/Infra/Http/`. A base `PipelineTest` sobe a API com as rotas do `PipelineTestController`, que existe só nos testes e entra por `AddApplicationPart`, e com `AddFakeLogging`. O teste herda `PipelineTest` e usa `Client`, autenticado, e `Logs` (o `FakeLogCollector`), sem limpeza do banco de dados. Uma rota nova no controller de teste provoca um erro que nenhum módulo provoca ainda. Exemplos: `SecurityHeadersTests`, `RequestLoggingTests` e `ErrorResponseTests`.

O `GetAsync` do `HttpClient` lê o corpo inteiro antes de devolver a resposta. Assim, o log da requisição já está no `Logs` quando o teste confere.

## DAO e Repository

Exemplo real: `backend/tests/VaccinationCard.IntegrationTests/Modules/Vaccines/Infra/Driven/Persistence/EfCore/VaccineDaoTests.cs`.

- O adaptador roda contra o banco de dados real: projeção, ordem, paginação, busca sem acento e tradução do `23505`.
- A classe tem construtor clássico: cria um escopo com `fixture.Factory.Services.CreateAsyncScope()` e o `_sut` com o `AppDbContext` desse escopo. O `DisposeAsync` descarta o escopo e chama a base.
- O Arrange grava pelo próprio adaptador (`_sut.SaveAsync`), e a conferência lê pelo `ExecuteDbContextAsync`, fora do escopo que gravou.
- Uma check constraint se prova com um valor fora do limite gravado pelo adaptador. A `DbUpdateException` traz uma `PostgresException` com `PostgresErrorCodes.CheckViolation` e o `ConstraintName` da configuração.
- A ordem se prova com uma lista fora de ordem, como as vacinas do PRD §9 em `UnorderedNames`. A busca se prova com maiúsculas, acentos e os curingas `%`, `_` e `\`.
- Num DAO só de leitura, o Arrange cadastra pelo caso de uso (`SendAsync`): `backend/tests/VaccinationCard.IntegrationTests/Modules/People/Infra/Driven/Persistence/EfCore/PersonDaoTests.cs`. A exceção é o teste do desempate por id: ele grava os registros direto na tabela, com ids escolhidos, na ordem contrária à dos ids, para a ordem de inserção não se passar pelo desempate.
- DAO que grava por SQL: `backend/tests/VaccinationCard.IntegrationTests/Modules/Authentication/Infra/Driven/Persistence/EfCore/SessionDaoTests.cs`. O upsert se prova com duas gravações em sequência e com duas ao mesmo tempo, em escopos diferentes: sobra uma sessão. Sem o change tracker, uma violação chega como `PostgresException`, e não como `DbUpdateException`. A chave entre módulos se prova nas duas pontas: a conta inexistente e a cascata ao remover a conta.
- Repository: `backend/tests/VaccinationCard.IntegrationTests/Modules/People/Infra/Driven/Persistence/EfCore/PersonRepositoryTests.cs`. O teste da leitura cadastra a pessoa em outro escopo (`SendAsync`), para a consulta ir ao banco de dados, e não ao registro que o `DbContext` do `_sut` já rastreia. O `EditAsync` se testa com a pessoa lida pelo `_sut`, alterada pelo agregado. A corrida de dois registros da mesma dose usa um segundo escopo, com outro `PersonRepository`: os dois leem a pessoa antes de qualquer gravação, e o `EditAsync` do segundo recebe o `23505` traduzido (RN11). A concorrência (RN16) usa a mesma montagem, com um teste para cada comando que acompanha a versão (`INSERT`, `UPDATE` e `DELETE`): o outro escopo grava primeiro, o `_sut` recebe o erro e a conferência mostra no banco de dados só a primeira alteração. A pessoa removida entre a leitura e a gravação sai de um `SendAsync` do `DeletePersonCommand`, depois do `FindByIdAsync` do `_sut`.

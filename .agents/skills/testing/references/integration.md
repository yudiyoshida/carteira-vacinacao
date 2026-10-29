# Teste de integração

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real.

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

```csharp
public sealed class CreateVaccineHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    [Trait("RN", "RN05")]
    public async Task Should_Reject_Vaccine_When_Name_Differs_Only_By_Case()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);

        // Act
        var act = () => SendAsync(new CreateVaccineCommand("hepatite b", 3, 0), cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<ConflictException>(act);
        exception.Code.ShouldBe("vaccine.name_already_registered");
    }
}
```

A base `IntegrationTest` limpa o banco de dados antes de cada teste e oferece dois helpers:

- `SendAsync`: envia o command ou a query num escopo de DI novo, como uma requisição HTTP, passando pelos behaviors do MediatR.
- `ExecuteDbContextAsync`: entrega um `AppDbContext` num escopo próprio, para preparar e conferir dados.

## Pipeline HTTP

Código em `backend/tests/VaccinationCard.IntegrationTests/Infra/Http/`. A base `PipelineTest` sobe a API com as rotas do `PipelineTestController`, que existe só nos testes e entra por `AddApplicationPart`, e com `AddFakeLogging`. O teste herda `PipelineTest` e usa `Client` e `Logs` (o `FakeLogCollector`), sem limpeza do banco de dados. Uma rota nova no controller de teste provoca um erro que nenhum módulo provoca ainda. Exemplos: `SecurityHeadersTests`, `RequestLoggingTests` e `ErrorResponseTests`.

O `GetAsync` do `HttpClient` lê o corpo inteiro antes de devolver a resposta. Assim, o log da requisição já está no `Logs` quando o teste confere.

## DAO e Repository

O adaptador roda contra o banco de dados real: projeção, ordem, paginação, busca sem acento e tradução do `23505`. O `_sut` recebe o `AppDbContext` de um escopo do próprio teste, criado no construtor e descartado no `DisposeAsync`. Para conferir o que foi gravado, leia pelo `ExecuteDbContextAsync`, fora do escopo que gravou.

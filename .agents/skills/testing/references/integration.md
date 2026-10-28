# Teste de integração

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real.

## Infraestrutura (T06)

- Um container PostgreSQL 18 por execução, como fixture de assembly do xUnit v3, com as migrations aplicadas uma vez.
- O Respawn limpa as tabelas antes de cada teste e preserva `__EFMigrationsHistory`.
- O projeto roda os testes em sequência, porque todos compartilham o mesmo banco de dados.
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

A base `IntegrationTest` limpa o banco de dados antes de cada teste e oferece `SendAsync`, que envia o command ou a query num escopo de DI novo, além de um `AppDbContext` para preparar e conferir dados.

## DAO e Repository

O adaptador roda contra o banco de dados real: projeção, ordem, paginação, busca sem acento e tradução do `23505`. Para conferir o que foi gravado, leia por um `AppDbContext` novo, fora do escopo que gravou.

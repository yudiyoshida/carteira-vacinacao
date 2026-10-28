# Teste HTTP

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real.

Os testes HTTP sobem a API com `WebApplicationFactory<Program>`, apontada para o PostgreSQL da fixture e com `FakeTimeProvider` no lugar do relógio. A partir da T19, o cliente padrão é autenticado (skill `authentication`).

```csharp
public sealed class VaccinesEndpointTests(IntegrationFixture fixture) : HttpTest(fixture)
{
    [Fact]
    public async Task Should_Return_Created_With_Location_When_Vaccine_Is_Valid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var body = new { name = "Hepatite B", primaryDoses = 3, boosterDoses = 0 };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/vaccines", body, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
    }

    [Fact]
    [Trait("RN", "RN07")]
    public async Task Should_Return_Method_Not_Allowed_When_Editing_A_Vaccine()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var url = $"/api/v1/vaccines/{Guid.CreateVersion7()}";

        // Act
        var response = await Client.PutAsJsonAsync(url, new { name = "Outra" }, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }
}
```

## O que conferir

- Cada status da linha da rota na TechSpec §7.2.
- Em erro: `Content-Type` `application/problem+json` e o `code` do corpo.
- Em `400`: as chaves de `errors` em camelCase, iguais aos campos do JSON.
- Nas rotas de autenticação: os atributos do `Set-Cookie`.

O ProblemDetails se lê com `ReadFromJsonAsync<JsonElement>`, e o código com `GetProperty("code").GetString()`.

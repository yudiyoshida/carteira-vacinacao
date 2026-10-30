using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;

namespace VaccinationCard.IntegrationTests.Modules.People.Infra.Drivers.Http;

public sealed class PeopleEndpointTests(IntegrationFixture fixture) : HttpTest(fixture)
{
    private const string Url = "/api/v1/people";

    [Fact]
    public async Task Should_Return_Created_With_Location_When_Person_Is_Valid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var body = new { name = "Maria Silva", cpf = "123.456.789-09" };

        // Act
        var response = await Client.PostAsJsonAsync(Url, body, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var id = created.GetProperty("id").GetGuid();
        response.Headers.Location.ShouldNotBeNull().AbsolutePath.ShouldBe($"{Url}/{id}");
    }

    [Fact]
    [Trait("RN", "RN01")]
    [Trait("RN", "RN02")]
    public async Task Should_Return_Bad_Request_With_Field_Errors_When_Person_Is_Invalid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var body = new { name = "  ", cpf = "111.111.111-11" };

        // Act
        var response = await Client.PostAsJsonAsync(Url, body, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").EnumerateObject().Select(field => field.Name).ShouldBe(["name", "cpf"], ignoreOrder: true);
    }

    [Fact]
    [Trait("RN", "RN03")]
    [Trait("RN", "RN25")]
    public async Task Should_Return_Conflict_Without_Personal_Data_When_Cpf_Is_Already_Registered()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var body = new { name = "Maria Souza", cpf = "123.456.789-09" };

        // Act
        var response = await Client.PostAsJsonAsync(Url, body, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("person.cpf_already_registered");
        var text = problem.GetRawText();
        text.ShouldNotContain("12345678909");
        text.ShouldNotContain("123.456.789-09");
        text.ShouldNotContain("Maria Souza");
    }

    [Fact]
    [Trait("RN", "RN26")]
    public async Task Should_Return_Person_With_Formatted_Cpf_When_It_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);

        // Act
        var response = await Client.GetAsync($"{Url}/{created.Id}", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var person = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        person.EnumerateObject().Select(field => field.Name).ShouldBe(["id", "name", "cpf"]);
        person.GetProperty("id").GetGuid().ShouldBe(created.Id);
        person.GetProperty("name").GetString().ShouldBe("Maria Silva");
        person.GetProperty("cpf").GetString().ShouldBe("123.456.789-09");
    }

    [Fact]
    public async Task Should_Return_Not_Found_When_Person_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.GetAsync($"{Url}/{Guid.CreateVersion7()}", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("person.not_found");
    }

    // The commands of EF Core carry the CPF and the name as parameters: their records must be in the log, without the
    // values.
    [Fact]
    [Trait("RN", "RN25")]
    public async Task Should_Log_Neither_Cpf_Nor_Name_When_Registering_A_Person()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = Fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureLogging(logging => logging.AddFakeLogging()));
        using var client = factory.CreateClient();
        var body = new { name = "Maria Silva", cpf = "123.456.789-09" };

        // Act
        var created = await client.PostAsJsonAsync(Url, body, cancellationToken);
        var repeated = await client.PostAsJsonAsync(Url, body, cancellationToken);

        // Assert
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        repeated.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var records = factory.Services.GetFakeLogCollector().GetSnapshot();
        records.ShouldContain(record => record.Category == DbLoggerCategory.Database.Command.Name);
        records.ShouldNotContain(record => record.Message.Contains("Maria Silva") || record.Message.Contains("12345678909") || record.Message.Contains("123.456.789-09"));
    }
}

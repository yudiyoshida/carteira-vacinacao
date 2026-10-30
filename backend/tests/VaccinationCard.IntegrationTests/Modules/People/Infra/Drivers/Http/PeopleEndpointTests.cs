using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.RegisterVaccination;
using VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;

namespace VaccinationCard.IntegrationTests.Modules.People.Infra.Drivers.Http;

public sealed class PeopleEndpointTests(IntegrationFixture fixture) : HttpTest(fixture)
{
    private const string Url = "/api/v1/people";

    // "page=abc" fails in model binding, before the validator; the others fail in the validator.
    public static TheoryData<string, string> InvalidListParameters => new()
    {
        { "size=0", "size" },
        { "size=101", "size" },
        { "page=0", "page" },
        { "page=abc", "page" },
        { $"search={new string('a', 101)}", "search" },
    };

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

    [Fact]
    [Trait("RN", "RN19")]
    [Trait("RN", "RN26")]
    public async Task Should_Return_Envelope_With_Masked_Cpf_When_Listing_People()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        await SendAsync(new CreatePersonCommand("Ana Conceição", "93541134780"), cancellationToken);

        // Act
        var response = await Client.GetAsync(Url, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        page.EnumerateObject().Select(field => field.Name).ShouldBe(["currentPage", "itemsPerPage", "totalItems", "totalPages", "data"]);
        page.GetProperty("currentPage").GetInt32().ShouldBe(1);
        page.GetProperty("itemsPerPage").GetInt32().ShouldBe(20);
        page.GetProperty("totalItems").GetInt32().ShouldBe(2);
        page.GetProperty("totalPages").GetInt32().ShouldBe(1);

        var data = page.GetProperty("data").EnumerateArray().ToList();
        data.Select(person => person.GetProperty("name").GetString()).ShouldBe(["Ana Conceição", "Maria Silva"]);
        
        var maria = data[1];
        maria.EnumerateObject().Select(field => field.Name).ShouldBe(["id", "name", "cpf"]);
        maria.GetProperty("id").GetGuid().ShouldBe(created.Id);
        maria.GetProperty("cpf").GetString().ShouldBe("***.456.789-**");
        
        var text = page.GetRawText();
        text.ShouldNotContain("12345678909");
        text.ShouldNotContain("123.456.789-09");
    }

    [Theory]
    [Trait("RN", "RN19")]
    [MemberData(nameof(InvalidListParameters))]
    public async Task Should_Return_Bad_Request_With_Field_Error_When_List_Parameter_Is_Invalid(string queryString, string field)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.GetAsync($"{Url}?{queryString}", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").EnumerateObject().Select(error => error.Name).ShouldBe([field]);
    }

    // The lookup after the removal is part of what is checked, so the Act makes both calls.
    [Fact]
    [Trait("RN", "RN04")]
    public async Task Should_Return_Success_Message_And_Then_Not_Found_When_Removing_A_Person()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);

        // Act
        var removed = await Client.DeleteAsync($"{Url}/{created.Id}", cancellationToken);
        var found = await Client.GetAsync($"{Url}/{created.Id}", cancellationToken);

        // Assert
        removed.StatusCode.ShouldBe(HttpStatusCode.OK);
        var message = await removed.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        message.EnumerateObject().Select(field => field.Name).ShouldBe(["message"]);
        message.GetProperty("message").GetString().ShouldBe("Pessoa removida com sucesso.");
        found.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        
        var problem = await ReadProblemAsync(found, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("person.not_found");
    }

    [Fact]
    [Trait("RN", "RN04")]
    public async Task Should_Remove_Vaccinations_When_Removing_A_Person()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        await SendAsync(new RegisterVaccinationCommand(created.Id, vaccine.Id, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15)), cancellationToken);

        // Act
        var response = await Client.DeleteAsync($"{Url}/{created.Id}", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var remains = await ExecuteDbContextAsync(db => db.Set<VaccinationRecord>().AnyAsync(x => x.PersonId == created.Id, cancellationToken));
        remains.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Return_Not_Found_When_Removing_A_Person_That_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.DeleteAsync($"{Url}/{Guid.CreateVersion7()}", cancellationToken);

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
        using var client = factory.CreateAuthenticatedClient();
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

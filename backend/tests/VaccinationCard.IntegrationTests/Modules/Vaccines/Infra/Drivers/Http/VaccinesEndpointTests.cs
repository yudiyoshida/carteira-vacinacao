using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;

namespace VaccinationCard.IntegrationTests.Modules.Vaccines.Infra.Drivers.Http;

public sealed class VaccinesEndpointTests(IntegrationFixture fixture) : HttpTest(fixture)
{
    private const string Url = "/api/v1/vaccines";

    [Fact]
    public async Task Should_Return_Created_With_Location_When_Vaccine_Is_Valid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var body = new { name = "Hepatite B", primaryDoses = 3, boosterDoses = 0 };

        // Act
        var response = await Client.PostAsJsonAsync(Url, body, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var id = created.GetProperty("id").GetGuid();
        response.Headers.Location.ShouldNotBeNull().AbsolutePath.ShouldBe($"{Url}/{id}");
    }

    [Fact]
    [Trait("RN", "RN05")]
    [Trait("RN", "RN06")]
    public async Task Should_Return_Bad_Request_With_Field_Errors_When_Vaccine_Is_Invalid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var body = new { name = " ", primaryDoses = 0, boosterDoses = 6 };

        // Act
        var response = await Client.PostAsJsonAsync(Url, body, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").EnumerateObject().Select(field => field.Name)
            .ShouldBe(["name", "primaryDoses", "boosterDoses"], ignoreOrder: true);
    }

    [Fact]
    [Trait("RN", "RN05")]
    public async Task Should_Return_Conflict_When_Name_Differs_Only_By_Case()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        var body = new { name = "hepatite b", primaryDoses = 3, boosterDoses = 0 };

        // Act
        var response = await Client.PostAsJsonAsync(Url, body, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("vaccine.name_already_registered");
    }

    [Fact]
    public async Task Should_Return_Vaccine_When_It_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);

        // Act
        var response = await Client.GetAsync($"{Url}/{created.Id}", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var vaccine = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        vaccine.EnumerateObject().Select(field => field.Name).ShouldBe(["id", "name", "primaryDoses", "boosterDoses"]);
        vaccine.GetProperty("id").GetGuid().ShouldBe(created.Id);
        vaccine.GetProperty("name").GetString().ShouldBe("Hepatite B");
        vaccine.GetProperty("primaryDoses").GetInt32().ShouldBe(3);
        vaccine.GetProperty("boosterDoses").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_Not_Found_When_Vaccine_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.GetAsync($"{Url}/{Guid.CreateVersion7()}", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("vaccine.not_found");
    }

    [Theory]
    [Trait("RN", "RN07")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task Should_Return_Method_Not_Allowed_When_Editing_A_Vaccine(string method)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        using var request = new HttpRequestMessage(new HttpMethod(method), $"{Url}/{created.Id}")
        {
            Content = JsonContent.Create(new { name = "Hepatite B", primaryDoses = 4, boosterDoses = 1 }),
        };

        // Act
        var response = await Client.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("route.method_not_allowed");
    }
}

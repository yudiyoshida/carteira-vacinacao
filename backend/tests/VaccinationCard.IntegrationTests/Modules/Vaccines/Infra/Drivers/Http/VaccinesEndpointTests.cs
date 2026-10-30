using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;

namespace VaccinationCard.IntegrationTests.Modules.Vaccines.Infra.Drivers.Http;

public sealed class VaccinesEndpointTests(IntegrationFixture fixture) : HttpTest(fixture)
{
    private const string Url = "/api/v1/vaccines";

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

    [Fact]
    [Trait("RN", "RN20")]
    public async Task Should_Return_Envelope_With_Every_Vaccine_When_Listing_Without_Pagination()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        await SendAsync(new CreateVaccineCommand("BCG", 1, 0), cancellationToken);

        // Act
        var response = await Client.GetAsync(Url, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        page.EnumerateObject().Select(field => field.Name).ShouldBe(["currentPage", "itemsPerPage", "totalItems", "totalPages", "data"]);
        page.GetProperty("currentPage").GetInt32().ShouldBe(1);
        page.GetProperty("itemsPerPage").GetInt32().ShouldBe(2);
        page.GetProperty("totalItems").GetInt32().ShouldBe(2);
        page.GetProperty("totalPages").GetInt32().ShouldBe(1);

        var data = page.GetProperty("data").EnumerateArray().ToList();
        data.Select(vaccine => vaccine.GetProperty("name").GetString()).ShouldBe(["BCG", "Hepatite B"]);
        
        var hepatiteB = data[1];
        hepatiteB.EnumerateObject().Select(field => field.Name).ShouldBe(["id", "name", "primaryDoses", "boosterDoses"]);
        hepatiteB.GetProperty("id").GetGuid().ShouldBe(created.Id);
        hepatiteB.GetProperty("primaryDoses").GetInt32().ShouldBe(3);
        hepatiteB.GetProperty("boosterDoses").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_Requested_Page_When_Listing_With_Page_And_Size()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        await SendAsync(new CreateVaccineCommand("BCG", 1, 0), cancellationToken);
        await SendAsync(new CreateVaccineCommand("DTP", 3, 2), cancellationToken);

        // Act
        var response = await Client.GetAsync($"{Url}?page=2&size=2", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        page.GetProperty("currentPage").GetInt32().ShouldBe(2);
        page.GetProperty("itemsPerPage").GetInt32().ShouldBe(2);
        page.GetProperty("totalItems").GetInt32().ShouldBe(3);
        page.GetProperty("totalPages").GetInt32().ShouldBe(2);
        page.GetProperty("data").EnumerateArray().Select(vaccine => vaccine.GetProperty("name").GetString()).ShouldBe(["Hepatite B"]);
    }

    [Theory]
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

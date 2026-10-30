using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.RegisterVaccination;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;

namespace VaccinationCard.IntegrationTests.Modules.People.Infra.Drivers.Http;

public sealed class VaccinationsEndpointTests(IntegrationFixture fixture) : HttpTest(fixture)
{
    private const string AnyVaccineId = "0192f1a0-7c3e-7000-8000-000000000001";

    // The validator rejects a missing field, a missing dose type (RN08) and a dose number below 1. An unknown type, a
    // number in place of the type name and a date in another format fail in model binding, before the validator.
    public static TheoryData<string, string[]> InvalidBodies => new()
    {
        { "{}", ["vaccineId", "dose", "appliedOn"] },
        { Body("""{ "type": "Primary", "number": 0 }""", "2020-01-15"), ["dose.number"] },
        { Body("""{ "number": 1 }""", "2020-01-15"), ["dose.type"] },
        { Body("""{ "type": "Third", "number": 1 }""", "2020-01-15"), ["dose.type"] },
        { Body("""{ "type": 1, "number": 1 }""", "2020-01-15"), ["dose.type"] },
        { Body("""{ "type": "Primary", "number": 1 }""", "15/01/2020"), ["appliedOn"] },
    };

    // The first dose of Hepatite B (three primary doses) is registered on 15/01/2020; each case breaks one rule.
    public static TheoryData<string, int, DateOnly, string> RuleBreakingDoses => new()
    {
        { "Primary", 4, new DateOnly(2020, 2, 15), "vaccination.dose_outside_scheme" },
        { "Primary", 3, new DateOnly(2020, 2, 15), "vaccination.dose_out_of_sequence" },
        { "Primary", 2, ApiFactory.Today.AddDays(1), "vaccination.applied_on_in_future" },
        { "Primary", 2, new DateOnly(1899, 12, 31), "vaccination.applied_on_before_minimum" },
        { "Primary", 2, new DateOnly(2020, 1, 15), "vaccination.applied_on_not_after_previous" },
    };

    [Fact]
    public async Task Should_Return_Created_With_Location_When_Vaccination_Is_Valid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var (personId, vaccineId) = await CreatePersonAndVaccineAsync(cancellationToken);
        var body = new { vaccineId, dose = new { type = "Primary", number = 1 }, appliedOn = "2020-01-15" };

        // Act
        var response = await Client.PostAsJsonAsync(Url(personId), body, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var id = created.GetProperty("id").GetGuid();
        response.Headers.Location.ShouldNotBeNull().AbsolutePath.ShouldBe($"{Url(personId)}/{id}");
    }

    [Theory]
    [Trait("RN", "RN08")]
    [Trait("RN", "RN12")]
    [MemberData(nameof(InvalidBodies))]
    public async Task Should_Return_Bad_Request_With_Field_Errors_When_Vaccination_Is_Invalid(string body, string[] fields)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync(Url(Guid.CreateVersion7()), content, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").EnumerateObject().Select(field => field.Name).ShouldBe(fields, ignoreOrder: true);
    }

    [Fact]
    public async Task Should_Return_Not_Found_When_Registering_For_A_Person_That_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var (_, vaccineId) = await CreatePersonAndVaccineAsync(cancellationToken);
        var body = new { vaccineId, dose = new { type = "Primary", number = 1 }, appliedOn = "2020-01-15" };

        // Act
        var response = await Client.PostAsJsonAsync(Url(Guid.CreateVersion7()), body, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("person.not_found");
    }

    [Fact]
    public async Task Should_Return_Not_Found_When_Vaccine_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var (personId, _) = await CreatePersonAndVaccineAsync(cancellationToken);
        var body = new { vaccineId = Guid.CreateVersion7(), dose = new { type = "Primary", number = 1 }, appliedOn = "2020-01-15" };

        // Act
        var response = await Client.PostAsJsonAsync(Url(personId), body, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("vaccine.not_found");
    }

    [Fact]
    [Trait("RN", "RN11")]
    public async Task Should_Return_Conflict_When_Dose_Is_Already_Registered()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var (personId, vaccineId) = await CreatePersonAndVaccineAsync(cancellationToken);
        await RegisterFirstDoseAsync(personId, vaccineId, cancellationToken);
        var body = new { vaccineId, dose = new { type = "Primary", number = 1 }, appliedOn = "2020-02-15" };

        // Act
        var response = await Client.PostAsJsonAsync(Url(personId), body, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("vaccination.dose_already_registered");
    }

    [Theory]
    [Trait("RN", "RN09")]
    [Trait("RN", "RN10")]
    [Trait("RN", "RN12")]
    [Trait("RN", "RN13")]
    [MemberData(nameof(RuleBreakingDoses))]
    public async Task Should_Return_Unprocessable_Entity_With_The_Rule_Code_When_Vaccination_Breaks_A_Rule(string type, int number, DateOnly appliedOn, string code)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var (personId, vaccineId) = await CreatePersonAndVaccineAsync(cancellationToken);
        await RegisterFirstDoseAsync(personId, vaccineId, cancellationToken);
        var body = new { vaccineId, dose = new { type, number }, appliedOn };

        // Act
        var response = await Client.PostAsJsonAsync(Url(personId), body, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe(code);
    }

    [Fact]
    public async Task Should_Return_Vaccination_When_It_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var (personId, vaccineId) = await CreatePersonAndVaccineAsync(cancellationToken);
        var registered = await RegisterFirstDoseAsync(personId, vaccineId, cancellationToken);

        // Act
        var response = await Client.GetAsync($"{Url(personId)}/{registered.Id}", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var vaccination = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        vaccination.EnumerateObject().Select(field => field.Name).ShouldBe(["id", "vaccineId", "dose", "appliedOn"]);
        vaccination.GetProperty("id").GetGuid().ShouldBe(registered.Id);
        vaccination.GetProperty("vaccineId").GetGuid().ShouldBe(vaccineId);
        
        var dose = vaccination.GetProperty("dose");
        dose.EnumerateObject().Select(field => field.Name).ShouldBe(["type", "number"]);
        dose.GetProperty("type").GetString().ShouldBe("Primary");
        dose.GetProperty("number").GetInt32().ShouldBe(1);
        vaccination.GetProperty("appliedOn").GetString().ShouldBe("2020-01-15");
    }

    [Fact]
    public async Task Should_Return_Not_Found_When_Reading_A_Vaccination_Of_A_Person_That_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.GetAsync($"{Url(Guid.CreateVersion7())}/{Guid.CreateVersion7()}", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("person.not_found");
    }

    [Fact]
    public async Task Should_Return_Not_Found_When_Vaccination_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var (personId, _) = await CreatePersonAndVaccineAsync(cancellationToken);

        // Act
        var response = await Client.GetAsync($"{Url(personId)}/{Guid.CreateVersion7()}", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("vaccination.not_found");
    }

    // Hepatite B (three primary doses) has its first two doses.
    [Fact]
    [Trait("RN", "RN17")]
    [Trait("RN", "RN26")]
    public async Task Should_Return_Vaccination_Card_With_The_Contract_Fields_When_Person_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var (personId, vaccineId) = await CreatePersonAndVaccineAsync(cancellationToken);
        var first = await RegisterFirstDoseAsync(personId, vaccineId, cancellationToken);
        var second = await SendAsync(new RegisterVaccinationCommand(personId, vaccineId, new DoseDto(DoseType.Primary, 2), new DateOnly(2020, 2, 15)), cancellationToken);

        // Act
        var response = await Client.GetAsync(CardUrl(personId), cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var card = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        card.EnumerateObject().Select(field => field.Name).ShouldBe(["person", "vaccines"]);

        var person = card.GetProperty("person");
        person.EnumerateObject().Select(field => field.Name).ShouldBe(["id", "name", "cpf"]);
        person.GetProperty("id").GetGuid().ShouldBe(personId);
        person.GetProperty("name").GetString().ShouldBe("Maria Silva");
        person.GetProperty("cpf").GetString().ShouldBe("123.456.789-09");

        var entry = card.GetProperty("vaccines").EnumerateArray().ShouldHaveSingleItem();
        entry.EnumerateObject().Select(field => field.Name).ShouldBe(["vaccineId", "vaccineName", "scheme", "doses", "progress", "nextExpectedDose"]);
        entry.GetProperty("vaccineId").GetGuid().ShouldBe(vaccineId);
        entry.GetProperty("vaccineName").GetString().ShouldBe("Hepatite B");
        
        var scheme = entry.GetProperty("scheme");
        scheme.EnumerateObject().Select(field => field.Name).ShouldBe(["primaryDoses", "boosterDoses"]);
        scheme.GetProperty("primaryDoses").GetInt32().ShouldBe(3);
        scheme.GetProperty("boosterDoses").GetInt32().ShouldBe(0);
        
        var doses = entry.GetProperty("doses").EnumerateArray().ToList();
        doses.Select(dose => dose.GetProperty("vaccinationId").GetGuid()).ShouldBe([first.Id, second.Id]);
        doses[0].EnumerateObject().Select(field => field.Name).ShouldBe(["vaccinationId", "dose", "appliedOn"]);
        
        var dose = doses[0].GetProperty("dose");
        dose.EnumerateObject().Select(field => field.Name).ShouldBe(["type", "number"]);
        dose.GetProperty("type").GetString().ShouldBe("Primary");
        dose.GetProperty("number").GetInt32().ShouldBe(1);
        doses[0].GetProperty("appliedOn").GetString().ShouldBe("2020-01-15");
        
        var progress = entry.GetProperty("progress");
        progress.EnumerateObject().Select(field => field.Name).ShouldBe(["primaryApplied", "primaryTotal", "primarySeriesComplete", "boostersApplied", "boostersTotal"]);
        progress.GetProperty("primaryApplied").GetInt32().ShouldBe(2);
        progress.GetProperty("primaryTotal").GetInt32().ShouldBe(3);
        progress.GetProperty("primarySeriesComplete").GetBoolean().ShouldBeFalse();
        progress.GetProperty("boostersApplied").GetInt32().ShouldBe(0);
        progress.GetProperty("boostersTotal").GetInt32().ShouldBe(0);
        
        var nextExpectedDose = entry.GetProperty("nextExpectedDose");
        nextExpectedDose.EnumerateObject().Select(field => field.Name).ShouldBe(["type", "number"]);
        nextExpectedDose.GetProperty("type").GetString().ShouldBe("Primary");
        nextExpectedDose.GetProperty("number").GetInt32().ShouldBe(3);
    }

    [Fact]
    public async Task Should_Return_Null_Next_Expected_Dose_When_Scheme_Is_Complete()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var person = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var bcg = await SendAsync(new CreateVaccineCommand("BCG", 1, 0), cancellationToken);
        await RegisterFirstDoseAsync(person.Id, bcg.Id, cancellationToken);

        // Act
        var response = await Client.GetAsync(CardUrl(person.Id), cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var card = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var entry = card.GetProperty("vaccines").EnumerateArray().ShouldHaveSingleItem();
        entry.GetProperty("nextExpectedDose").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Should_Return_Card_Without_Vaccines_When_Person_Has_No_Vaccination()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var (personId, _) = await CreatePersonAndVaccineAsync(cancellationToken);

        // Act
        var response = await Client.GetAsync(CardUrl(personId), cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var card = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        card.GetProperty("person").GetProperty("id").GetGuid().ShouldBe(personId);
        card.GetProperty("vaccines").EnumerateArray().ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Return_Not_Found_When_Reading_The_Card_Of_A_Person_That_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.GetAsync(CardUrl(Guid.CreateVersion7()), cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("person.not_found");
    }

    private static string Url(Guid personId) => $"/api/v1/people/{personId}/vaccinations";

    private static string CardUrl(Guid personId) => $"/api/v1/people/{personId}/vaccination-card";

    private static string Body(string dose, string appliedOn) =>
        $$"""{ "vaccineId": "{{AnyVaccineId}}", "dose": {{dose}}, "appliedOn": "{{appliedOn}}" }""";

    private async Task<(Guid PersonId, Guid VaccineId)> CreatePersonAndVaccineAsync(CancellationToken cancellationToken)
    {
        var person = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        return (person.Id, vaccine.Id);
    }

    private Task<RegisterVaccinationOutput> RegisterFirstDoseAsync(Guid personId, Guid vaccineId, CancellationToken cancellationToken)
    {
        return SendAsync(new RegisterVaccinationCommand(personId, vaccineId, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15)), cancellationToken);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using VaccinationCard.Api.Infra.Database.Seeds;
using VaccinationCard.Api.Modules.Accounts.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.DeletePerson;
using VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Modules.Vaccines.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.IntegrationTests.Infra.Database.Seeds;

public sealed class DatabaseSeedTests(IntegrationFixture fixture) : HttpTest(fixture)
{
    private const string DemoEmail = "operador@demo.local";
    private const string DemoPassword = "Demo@2026!";
    private const int DemoVaccineCount = 8;
    private const int DemoVaccinationCount = 7;

    // The table of PRD §9, written here and not read from the seed, so a wrong vaccine in the seed fails the test.
    private static readonly (string Name, short PrimaryDoses, short BoosterDoses)[] PrdVaccines =
    [
        ("BCG", 1, 0),
        ("Hepatite B", 3, 0),
        ("DTP", 3, 2),
        ("Poliomielite inativada (VIP)", 3, 1),
        ("Pneumocócica 10-valente", 2, 1),
        ("Meningocócica C", 2, 1),
        ("Tríplice viral", 2, 0),
        ("Febre amarela", 1, 0),
    ];

    [Fact]
    public async Task Should_Not_Seed_When_The_Key_Keeps_The_Default_Value()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = Fixture.Factory.WithWebHostBuilder(_ => { });

        // Act
        _ = factory.Services;

        // Assert
        (await ReadAllIdsAsync(cancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Seed_Account_Vaccines_And_Person_When_The_Database_Is_Empty()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await StartApiWithSeedAsync();

        // Assert
        var account = await ExecuteDbContextAsync(db => db.Set<AccountRecord>().SingleAsync(cancellationToken));
        account.Email.ShouldBe(DemoEmail);
        account.PasswordHash.ShouldNotContain(DemoPassword);

        var vaccines = await ExecuteDbContextAsync(db => db.Set<VaccineRecord>().ToListAsync(cancellationToken));
        vaccines.Select(x => (x.Name, x.PrimaryDoses, x.BoosterDoses)).ShouldBe(PrdVaccines, ignoreOrder: true);

        var person = await ExecuteDbContextAsync(db => db.Set<PersonRecord>().SingleAsync(cancellationToken));
        person.Name.ShouldBe("Maria Demonstração");
        person.Cpf.ShouldBe("12345678909");
        (await CountAsync<VaccinationRecord>(cancellationToken)).ShouldBe(DemoVaccinationCount);
    }

    [Fact]
    public async Task Should_Not_Duplicate_Data_When_The_Seed_Runs_Twice()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await StartApiWithSeedAsync();
        var idsAfterFirstRun = await ReadAllIdsAsync(cancellationToken);

        // Act
        await StartApiWithSeedAsync();

        // Assert
        idsAfterFirstRun.Count.ShouldBe(1 + DemoVaccineCount + 1 + DemoVaccinationCount);
        (await ReadAllIdsAsync(cancellationToken)).ShouldBe(idsAfterFirstRun);
    }

    [Fact]
    public async Task Should_Build_A_Partial_Card_That_Follows_The_Rules_When_The_Person_Is_Seeded()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await StartApiWithSeedAsync();
        var personId = await ExecuteDbContextAsync(db => db.Set<PersonRecord>().Select(x => x.Id).SingleAsync(cancellationToken));

        // Act
        var response = await Client.GetAsync($"/api/v1/people/{personId}/vaccination-card", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var card = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var person = card.GetProperty("person");
        person.GetProperty("name").GetString().ShouldBe("Maria Demonstração");
        person.GetProperty("cpf").GetString().ShouldBe("123.456.789-09");

        var vaccines = card.GetProperty("vaccines").EnumerateArray().ToList();
        vaccines.Select(entry => entry.GetProperty("vaccineName").GetString()).ShouldBe(["BCG", "DTP", "Hepatite B"]);

        ReadDoses(vaccines[0]).ShouldBe(["Primary 1"]);
        ReadProgress(vaccines[0]).ShouldBe((1, 1, true, 0, 0));
        vaccines[0].GetProperty("nextExpectedDose").ValueKind.ShouldBe(JsonValueKind.Null);

        ReadDoses(vaccines[1]).ShouldBe(["Primary 1", "Primary 2", "Primary 3", "Booster 1"]);
        ReadProgress(vaccines[1]).ShouldBe((3, 3, true, 1, 2));
        ReadDose(vaccines[1].GetProperty("nextExpectedDose")).ShouldBe("Booster 2");

        ReadDoses(vaccines[2]).ShouldBe(["Primary 1", "Primary 2"]);
        ReadProgress(vaccines[2]).ShouldBe((2, 3, false, 0, 0));
        ReadDose(vaccines[2].GetProperty("nextExpectedDose")).ShouldBe("Primary 3");

        // Each date comes after the previous dose and is not in the future.
        foreach (var entry in vaccines)
        {
            var dates = entry.GetProperty("doses").EnumerateArray().Select(dose => dose.GetProperty("appliedOn").Deserialize<DateOnly>()).ToList();
            dates.Zip(dates.Skip(1)).ShouldAllBe(pair => pair.First < pair.Second);
            dates.ShouldAllBe(date => date <= ApiFactory.Today);
        }
    }

    [Fact]
    public async Task Should_Sign_In_When_Using_The_Demo_Account()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await StartApiWithSeedAsync();

        // Act
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", new { email = DemoEmail, password = DemoPassword }, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        body.GetProperty("accessToken").GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Keep_The_Accounts_When_The_Table_Has_Data()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await CreateAccountAsync("outra@conta.local", "Outra@2026!", cancellationToken);

        // Act
        await StartApiWithSeedAsync();

        // Assert
        var account = await ExecuteDbContextAsync(db => db.Set<AccountRecord>().SingleAsync(cancellationToken));
        account.Id.ShouldBe(accountId);
        (await CountAsync<VaccineRecord>(cancellationToken)).ShouldBe(DemoVaccineCount);
        (await CountAsync<PersonRecord>(cancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Keep_The_People_When_The_Table_Has_Data()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var existing = await SendAsync(new CreatePersonCommand("Joana Silva", "52998224725"), cancellationToken);

        // Act
        await StartApiWithSeedAsync();

        // Assert
        var person = await ExecuteDbContextAsync(db => db.Set<PersonRecord>().SingleAsync(cancellationToken));
        person.Id.ShouldBe(existing.Id);
        (await CountAsync<VaccinationRecord>(cancellationToken)).ShouldBe(0);
        (await CountAsync<VaccineRecord>(cancellationToken)).ShouldBe(DemoVaccineCount);
        (await CountAsync<AccountRecord>(cancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Seed_The_Demo_Person_Again_When_The_People_Table_Becomes_Empty()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await StartApiWithSeedAsync();
        var removedId = await ExecuteDbContextAsync(db => db.Set<PersonRecord>().Select(x => x.Id).SingleAsync(cancellationToken));
        await SendAsync(new DeletePersonCommand(removedId), cancellationToken);

        // Act
        await StartApiWithSeedAsync();

        // Assert
        var person = await ExecuteDbContextAsync(db => db.Set<PersonRecord>().SingleAsync(cancellationToken));
        person.Id.ShouldNotBe(removedId);
        (await CountAsync<VaccinationRecord>(cancellationToken)).ShouldBe(DemoVaccinationCount);
        (await CountAsync<VaccineRecord>(cancellationToken)).ShouldBe(DemoVaccineCount);
        (await CountAsync<AccountRecord>(cancellationToken)).ShouldBe(1);
    }

    [Theory]
    [InlineData("Outra vacina", 1, 0)]
    [InlineData("DTP", 3, 0)]
    public async Task Should_Start_Without_The_Demo_Person_When_The_Vaccines_Table_Lacks_A_Demo_Vaccine(string name, int primaryDoses, int boosterDoses)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var existing = await SendAsync(new CreateVaccineCommand(name, primaryDoses, boosterDoses), cancellationToken);
        await using var factory = Fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Database:SeedOnStartup", "true");
            builder.ConfigureLogging(logging => logging.AddFakeLogging());
        });

        // Act
        _ = factory.Services;

        // Assert
        var vaccine = await ExecuteDbContextAsync(db => db.Set<VaccineRecord>().SingleAsync(cancellationToken));
        vaccine.Id.ShouldBe(existing.Id);
        (await CountAsync<PersonRecord>(cancellationToken)).ShouldBe(0);
        (await CountAsync<AccountRecord>(cancellationToken)).ShouldBe(1);
        factory.Services.GetFakeLogCollector().GetSnapshot().ShouldContain(record => record.Category == typeof(PersonSeed).FullName && record.Level == LogLevel.Warning);
    }

    // On this clock, the dates of the demo doses are in the future. The aggregate refuses the first one (RN12) after the
    // account, the vaccines and the person were written, and the transaction undoes them.
    [Fact]
    public async Task Should_Leave_No_Data_When_The_Aggregate_Refuses_A_Dose()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = Fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Database:SeedOnStartup", "true");
            builder.ConfigureTestServices(services =>
                services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeSpan.Zero))));
        });

        // Act
        var exception = Should.Throw<BusinessRuleException>(() => factory.Services);

        // Assert
        exception.Code.ShouldBe("vaccination.applied_on_in_future");
        (await ReadAllIdsAsync(cancellationToken)).ShouldBeEmpty();
    }

    // Each start runs the startup of Program again, as a restart of the API does.
    private async Task StartApiWithSeedAsync()
    {
        await using var factory = Fixture.Factory.WithWebHostBuilder(builder => builder.UseSetting("Database:SeedOnStartup", "true"));
        _ = factory.Services;
    }

    private Task<int> CountAsync<TRecord>(CancellationToken cancellationToken) where TRecord : class
    {
        return ExecuteDbContextAsync(db => db.Set<TRecord>().CountAsync(cancellationToken));
    }

    private Task<List<Guid>> ReadAllIdsAsync(CancellationToken cancellationToken)
    {
        return ExecuteDbContextAsync(async db =>
        {
            List<Guid> ids = [];
            ids.AddRange(await db.Set<AccountRecord>().OrderBy(x => x.Id).Select(x => x.Id).ToListAsync(cancellationToken));
            ids.AddRange(await db.Set<VaccineRecord>().OrderBy(x => x.Id).Select(x => x.Id).ToListAsync(cancellationToken));
            ids.AddRange(await db.Set<PersonRecord>().OrderBy(x => x.Id).Select(x => x.Id).ToListAsync(cancellationToken));
            ids.AddRange(await db.Set<VaccinationRecord>().OrderBy(x => x.Id).Select(x => x.Id).ToListAsync(cancellationToken));
            return ids;
        });
    }

    private static List<string> ReadDoses(JsonElement entry) =>
        [.. entry.GetProperty("doses").EnumerateArray().Select(dose => ReadDose(dose.GetProperty("dose")))];

    private static string ReadDose(JsonElement dose) => $"{dose.GetProperty("type").GetString()} {dose.GetProperty("number").GetInt32()}";

    private static (int PrimaryApplied, int PrimaryTotal, bool PrimarySeriesComplete, int BoostersApplied, int BoostersTotal) ReadProgress(JsonElement entry)
    {
        var progress = entry.GetProperty("progress");
        return (
            progress.GetProperty("primaryApplied").GetInt32(),
            progress.GetProperty("primaryTotal").GetInt32(),
            progress.GetProperty("primarySeriesComplete").GetBoolean(),
            progress.GetProperty("boostersApplied").GetInt32(),
            progress.GetProperty("boostersTotal").GetInt32()
        );
    }
}

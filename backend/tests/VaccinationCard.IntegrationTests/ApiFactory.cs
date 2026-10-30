using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace VaccinationCard.IntegrationTests;

public sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    // "Today" for the API in the tests (RN12): the clock stops at 12:00 UTC, 09:00 in Brasília, on this date. The date
    // is in the past, so a date after it, which the API must refuse, would pass with the real clock.
    public static readonly DateOnly Today = new(2025, 6, 15);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("OpenApi:Enabled", "true");
        builder.UseSetting("App:TimeZone", "America/Sao_Paulo");
        builder.ConfigureTestServices(services =>
            services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(Today, new TimeOnly(12, 0), TimeSpan.Zero))));
    }
}

using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Infra.Database.Seeds;

namespace VaccinationCard.Api.Infra.Database;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>((provider, options) => options
            .UseNpgsql(provider.GetRequiredService<IConfiguration>().GetConnectionString("Default") ?? throw new InvalidOperationException("A connection string 'Default' não está configurada."))
            .UseSnakeCaseNamingConvention());

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

        services.AddScoped<AccountSeed>();
        services.AddScoped<VaccineSeed>();
        services.AddScoped<PersonSeed>();

        return services;
    }

    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:SeedOnStartup"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;

        // The seeds share the DbContext of the scope, so one transaction covers them: a failure leaves no partial card,
        // and the next start runs them again (TechSpec §6.4).
        await using var transaction = await services.GetRequiredService<AppDbContext>().Database.BeginTransactionAsync();
        await services.GetRequiredService<AccountSeed>().RunAsync(CancellationToken.None);
        await services.GetRequiredService<VaccineSeed>().RunAsync(CancellationToken.None);
        await services.GetRequiredService<PersonSeed>().RunAsync(CancellationToken.None);
        await transaction.CommitAsync();
    }
}

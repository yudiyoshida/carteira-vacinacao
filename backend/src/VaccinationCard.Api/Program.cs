using VaccinationCard.Api.Core;
using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Infra.Http;
using VaccinationCard.Api.Infra.OpenApi;
using VaccinationCard.Api.Modules.Accounts;
using VaccinationCard.Api.Modules.Authentication;
using VaccinationCard.Api.Modules.People;
using VaccinationCard.Api.Modules.Vaccines;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
});

// The hosting log writes the query string, which can carry a person's name (RN25); the request log replaces it.
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);

builder.Services.AddCore();
builder.Services.AddHttpPipeline();
builder.Services.AddOpenApiDocumentation();
builder.Services.AddDatabase();

builder.Services.AddAccountsModule();
builder.Services.AddAuthenticationModule();
builder.Services.AddPeopleModule();
builder.Services.AddVaccinesModule();

var app = builder.Build();

await app.MigrateDatabaseAsync();
await app.SeedDatabaseAsync();

app.UseHttpPipeline();
app.MapOpenApiDocumentation();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

await app.RunAsync();

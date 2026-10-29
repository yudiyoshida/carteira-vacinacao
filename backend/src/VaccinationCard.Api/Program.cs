using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Infra.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
});

builder.Services.AddControllers();
builder.Services.AddOpenApiDocumentation();
builder.Services.AddDatabase();

var app = builder.Build();

await app.MigrateDatabaseAsync();

app.MapOpenApiDocumentation();
app.MapHealthChecks("/health");
app.MapControllers();

await app.RunAsync();

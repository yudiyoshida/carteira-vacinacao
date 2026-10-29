using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace VaccinationCard.IntegrationTests;

public sealed class IntegrationFixture : IAsyncLifetime
{
    private static readonly RespawnerOptions RespawnerOptions = new()
    {
        DbAdapter = DbAdapter.Postgres,
        SchemasToInclude = ["public"],
        TablesToIgnore = ["__EFMigrationsHistory"],
    };

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("vaccination_card")
        .Build();

    private ApiFactory? _factory;
    private Respawner? _respawner;

    public ApiFactory Factory => _factory ?? throw new InvalidOperationException("The integration fixture has not started.");

    public string ConnectionString => _database.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync(TestContext.Current.CancellationToken);
        _factory = new ApiFactory(ConnectionString);

        // Starting the API applies the migrations, once for the whole run.
        _ = _factory.Services;
    }

    public async Task ResetDatabaseAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        // Respawn refuses a schema without tables, so it is created on the first reset.
        _respawner ??= await Respawner.CreateAsync(connection, RespawnerOptions);
        await _respawner.ResetAsync(connection);
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _database.DisposeAsync();
    }
}

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Dapper;
using DbUp;
using DbUp.Engine;
using Npgsql;
using NpgsqlTypes;
using OutboxProcessorWorker.Domain;

namespace OutboxProcessorWorker.Database;

public sealed class NpgsqlDatabaseInitializer(
    IConnectionStringProvider _connectionStringProvider,
    ILogger<NpgsqlDatabaseInitializer> _logger) : IDatabaseInitializer
{
    private readonly string _connectionString = _connectionStringProvider.ConnectionString;

    public async Task Execute()
    {
        _logger.LogInformation("Starting database initialization.");

        initializeDatabase();

        await seedInitialData();

        _logger.LogInformation("Database initialization completed successfully.");
    }

    private void initializeDatabase()
    {
        EnsureDatabase.For.PostgresqlDatabase(_connectionString);

        UpgradeEngine upgrader = DeployChanges.To
            .PostgresqlDatabase(_connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), scriptName => scriptName.Contains("postgres_"))
            .JournalToPostgresqlTable("public", "__migrations_history")
            .WithTransaction()
            .LogToConsole()
            .Build();

        DatabaseUpgradeResult result = upgrader.PerformUpgrade();

        if (result.Successful)
        {
            _logger.LogInformation("Database upgrade completed successfully");
        }
        else
        {
            _logger.LogError("Database upgrade failed with error: '{Error}'", result.Error);
        }
    }

    private async Task seedInitialData()
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        await connection.OpenAsync();

        _logger.LogInformation("Deleting existing records from outbox_messages table.");
        await connection.ExecuteAsync("TRUNCATE TABLE outbox_messages");

        const int batchSize    = IDatabaseInitializer.BatchSize;
        const int totalRecords = IDatabaseInitializer.TotalRecords;

        _logger.LogInformation("Seeding {Amount:N0} records to outbox_messages table.", totalRecords);

        const string sql = "COPY outbox_messages (id, type, content, occurred_on_utc) FROM STDIN (FORMAT BINARY)";

        long startingTimestamp = Stopwatch.GetTimestamp();

        await using NpgsqlBinaryImporter binaryImporter = await connection.BeginBinaryImportAsync(sql);

        for (int i = 0; i < totalRecords; i++)
        {
            string eventJson = JsonSerializer.Serialize(OrderCreatedEvent.CreateNew());

            await binaryImporter.StartRowAsync();

            await binaryImporter.WriteAsync(Guid.NewGuid(),             NpgsqlDbType.Uuid);
            await binaryImporter.WriteAsync(OrderCreatedEvent.FullName, NpgsqlDbType.Varchar);
            await binaryImporter.WriteAsync(eventJson,                  NpgsqlDbType.Jsonb);
            await binaryImporter.WriteAsync(DateTime.UtcNow,            NpgsqlDbType.TimestampTz);

            if ((i + 1) % batchSize == 0)
            {
                _logger.LogInformation("Inserted {Count:N0} records", i + 1);
            }
        }

        await binaryImporter.CompleteAsync();

        _logger.LogInformation("Finished seeding {Amount:N0} records in {Elapsed}.", totalRecords, Stopwatch.GetElapsedTime(startingTimestamp));
    }
}

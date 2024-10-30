using System.Diagnostics;
using System.Text.Json;
using Dapper;
using Npgsql;
using NpgsqlTypes;
using OutboxProcessorWorker.Domain;

namespace OutboxProcessorWorker.Database;

public sealed class NpgsqlDatabaseInitializer : IDatabaseInitializer
{
    private readonly ILogger<NpgsqlDatabaseInitializer> _logger;
    private readonly NpgsqlDataSource _npgsqlDataSource;

    public NpgsqlDatabaseInitializer(IConnectionStringProvider connectionStringProvider, ILogger<NpgsqlDatabaseInitializer> logger)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionStringProvider.ConnectionString);

        _logger           = logger;
        _npgsqlDataSource = dataSourceBuilder.Build();
    }

    public async Task Execute()
    {
        try
        {
            _logger.LogInformation("Starting database initialization.");

            await initializeDatabase();

            await seedInitialData();

            _logger.LogInformation("Database initialization completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing the database.");
        }
    }

    private async Task initializeDatabase()
    {
        const string sql =
            """
            -- Create outbox_messages table if it does not exist
            CREATE TABLE IF NOT EXISTS outbox_messages (
                id UUID PRIMARY KEY,
                type VARCHAR(255) NOT NULL,
                content JSONB NOT NULL,
                occurred_on_utc TIMESTAMP WITH TIME ZONE NOT NULL,
                processed_on_utc TIMESTAMP WITH TIME ZONE NULL,
                error TEXT NULL
            );
            
            -- Create a filtered index on unprocessed messages, including all necessary columns
            CREATE INDEX IF NOT EXISTS idx_outbox_messages_unprocessed 
                ON outbox_messages (occurred_on_utc, processed_on_utc)
                INCLUDE (id, type, content)
                WHERE processed_on_utc IS NULL;
            """;

        await using NpgsqlConnection connection = await _npgsqlDataSource.OpenConnectionAsync();

        await connection.ExecuteAsync(sql);
    }

    private async Task seedInitialData()
    {
        await using NpgsqlConnection connection = await _npgsqlDataSource.OpenConnectionAsync();

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

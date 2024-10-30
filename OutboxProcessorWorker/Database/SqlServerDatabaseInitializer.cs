using System.Data;
using System.Diagnostics;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using OutboxProcessorWorker.Domain;

namespace OutboxProcessorWorker.Database;

public sealed class SqlServerDatabaseInitializer(
    IConnectionStringProvider _connectionStringProvider,
    ILogger<NpgsqlDatabaseInitializer> _logger) : IDatabaseInitializer
{
    private static int _totalInsertedRecords = 0;

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
            -- Create OutboxMessages table if it does not exist
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OutboxMessages')
            BEGIN
            CREATE TABLE OutboxMessages (
                [Id] UNIQUEIDENTIFIER PRIMARY KEY,
                [Type] VARCHAR(255) NOT NULL,
                [Content] VARCHAR(MAX) NOT NULL,
                [ProcessedOnUtc] DATETIME,
                [Error] NVARCHAR(MAX),
                [OccurredOnUtc] DATETIME NOT NULL
                );
            END
            
            -- Create a filtered index on unprocessed messages, including all necessary columns
            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'idx_OutboxMessages_unprocessed'
                AND object_id = OBJECT_ID('dbo.OutboxMessages')
            )
            BEGIN
                CREATE NONCLUSTERED INDEX idx_OutboxMessages_unprocessed
                ON dbo.OutboxMessages ([OccurredOnUtc], [ProcessedOnUtc])
                INCLUDE ([Id], [Type], [Content])
                WHERE [ProcessedOnUtc] IS NULL;
            END
            """;

        await using var connection = new SqlConnection(_connectionStringProvider.ConnectionString);

        await connection.OpenAsync();

        await connection.ExecuteAsync(sql);
    }

    private async Task seedInitialData()
    {
        await using (var connection = new SqlConnection(_connectionStringProvider.ConnectionString))
        {
            await connection.OpenAsync();

            _logger.LogInformation("Deleting existing records from OutboxMessages table.");

            await connection.ExecuteAsync("TRUNCATE TABLE OutboxMessages");
        }

        const int batchSize    = IDatabaseInitializer.BatchSize;
        const int totalRecords = IDatabaseInitializer.TotalRecords;

        _logger.LogInformation("Seeding {Amount:N0} records to OutboxMessages table.", totalRecords);

        long startingTimestamp = Stopwatch.GetTimestamp();

        using var bulkCopy = new SqlBulkCopy(_connectionStringProvider.ConnectionString);

        bulkCopy.DestinationTableName = "OutboxMessages";

        bulkCopy.ColumnMappings.Add(nameof(OutboxMessage.Id),            nameof(OutboxMessage.Id));
        bulkCopy.ColumnMappings.Add(nameof(OutboxMessage.Type),          nameof(OutboxMessage.Type));
        bulkCopy.ColumnMappings.Add(nameof(OutboxMessage.Content),       nameof(OutboxMessage.Content));
        bulkCopy.ColumnMappings.Add(nameof(OutboxMessage.OccurredOnUtc), nameof(OutboxMessage.OccurredOnUtc));

        while (_totalInsertedRecords < totalRecords)
        {
            DataTable dataTable = get_OutboxMessage_DataTable(batchSize, totalRecords);

            await bulkCopy.WriteToServerAsync(dataTable);

            _logger.LogInformation("Inserted {Count:N0} records", _totalInsertedRecords);
        }

        _logger.LogInformation("Finished seeding {Amount:N0} records in {Elapsed}.", totalRecords, Stopwatch.GetElapsedTime(startingTimestamp));
    }

    private static DataTable get_OutboxMessage_DataTable(int batchSize, int totalRecords)
    {
        var dataTable = new DataTable();

        dataTable.Columns.Add(nameof(OutboxMessage.Id),            typeof(Guid));
        dataTable.Columns.Add(nameof(OutboxMessage.Type),          typeof(string));
        dataTable.Columns.Add(nameof(OutboxMessage.Content),       typeof(string));
        dataTable.Columns.Add(nameof(OutboxMessage.OccurredOnUtc), typeof(DateTime));

        string eventJson = JsonSerializer.Serialize(OrderCreatedEvent.CreateNew());

        for (int i = 0; i < batchSize && (_totalInsertedRecords + i) < totalRecords; i++, _totalInsertedRecords++)
        {
            dataTable.Rows.Add(Guid.NewGuid(), OrderCreatedEvent.FullName, eventJson, DateTime.UtcNow);
        }

        return dataTable;
    }
}

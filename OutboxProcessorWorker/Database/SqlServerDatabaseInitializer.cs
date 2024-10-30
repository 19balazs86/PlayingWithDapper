using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Dapper;
using DbUp;
using DbUp.Engine;
using Microsoft.Data.SqlClient;
using OutboxProcessorWorker.Domain;

namespace OutboxProcessorWorker.Database;

public sealed class SqlServerDatabaseInitializer(
    IConnectionStringProvider _connectionStringProvider,
    ILogger<NpgsqlDatabaseInitializer> _logger) : IDatabaseInitializer
{
    private readonly string _connectionString = _connectionStringProvider.ConnectionString;

    private static int _totalInsertedRecords = 0;

    public async Task Execute()
    {
        _logger.LogInformation("Starting database initialization.");

        initializeDatabase();

        await seedInitialData();

        _logger.LogInformation("Database initialization completed successfully.");
    }

    private void initializeDatabase()
    {
        EnsureDatabase.For.SqlDatabase(_connectionString);

        UpgradeEngine upgrader = DeployChanges.To
            .SqlDatabase(_connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), scriptName => scriptName.Contains("sql_"))
            .JournalToSqlTable("dbo", "__migrations_history")
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
        await using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            _logger.LogInformation("Deleting existing records from OutboxMessages table.");

            await connection.ExecuteAsync("TRUNCATE TABLE OutboxMessages");
        }

        const int batchSize    = IDatabaseInitializer.BatchSize;
        const int totalRecords = IDatabaseInitializer.TotalRecords;

        _logger.LogInformation("Seeding {Amount:N0} records to OutboxMessages table.", totalRecords);

        long startingTimestamp = Stopwatch.GetTimestamp();

        using var bulkCopy = new SqlBulkCopy(_connectionString);

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

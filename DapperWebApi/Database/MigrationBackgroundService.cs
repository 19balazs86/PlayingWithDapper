using System.Reflection;
using DbUp;
using DbUp.Engine;
using Npgsql;
using NpgsqlTypes;

namespace DapperWebApi.Database;

public sealed class MigrationBackgroundService(
    IConnectionStringProvider _connectionStringProvider,
    ILogger<MigrationBackgroundService> _logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Run(() => DatabaseMigration.Run(_connectionStringProvider.ConnectionString, _logger), stoppingToken);

        // await DatabaseMigration.ExampleBinaryImport(_connectionStringProvider.ConnectionString);
    }
}

public static class DatabaseMigration
{
    public static void Run(string connectionString, ILogger logger)
    {
        try
        {
            EnsureDatabase.For.PostgresqlDatabase(connectionString);

            UpgradeEngine upgrader = DeployChanges.To
                .PostgresqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                .JournalToPostgresqlTable("public", "__migration_history")
                .WithTransaction()
                .LogToConsole()
                .Build();

            DatabaseUpgradeResult result = upgrader.PerformUpgrade();

            if (result.Successful)
            {
                logger.LogInformation("Database upgrade completed successfully");
            }
            else
            {
                logger.LogError("Database upgrade failed with error: '{Error}'", result.Error);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Migration failed to run");
        }
    }

    public static async Task ExampleBinaryImport(string connectionString)
    {
        const string sql = "COPY bookings (room_id, start_date, end_date, total_price) FROM STDIN (FORMAT BINARY)";

        await using var connection = new NpgsqlConnection(connectionString);

        await connection.OpenAsync();

        // High-performance bulk data import of binary data directly into Postgres
        // Bulk inserts using binary format are significantly faster compared text-based bulk loading
        await using NpgsqlBinaryImporter binaryImporter = await connection.BeginBinaryImportAsync(sql);

        for (int roomId = 1; roomId <= 10; roomId++)
        {
            await binaryImporter.StartRowAsync();

            await binaryImporter.WriteAsync(roomId,                          NpgsqlDbType.Integer);
            await binaryImporter.WriteAsync(DateTime.UtcNow.Date,            NpgsqlDbType.Date);
            await binaryImporter.WriteAsync(DateTime.UtcNow.Date.AddDays(5), NpgsqlDbType.Date);
            await binaryImporter.WriteAsync(123.45,                          NpgsqlDbType.Numeric);
        }

        await binaryImporter.CompleteAsync();
    }
}

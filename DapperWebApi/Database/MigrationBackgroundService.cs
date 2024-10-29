using System.Reflection;
using DbUp;
using DbUp.Engine;

namespace DapperWebApi.Database;

public sealed class MigrationBackgroundService(
    IConnectionStringProvider _connectionStringProvider,
    ILogger<MigrationBackgroundService> _logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Run(() => DatabaseMigration.Run(_connectionStringProvider.ConnectionString, _logger), stoppingToken);
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
}

using Npgsql;
using OutboxProcessorWorker.Database;
using System.Data.Common;

namespace OutboxProcessorWorker.Outbox;

public class OutboxProcessor_Npgsql : OutboxProcessor_Base
{
    private readonly string _connectionString;

    private readonly NpgsqlDataSource _npgsqlDataSource;

    public OutboxProcessor_Npgsql(
        IConnectionStringProvider       connectionStringProvider,
        ILogger<OutboxProcessor_Npgsql> logger,
        IMessagePublisher               messagePublisher) : base(logger, messagePublisher)
    {
        _connectionString = connectionStringProvider.ConnectionString;

        var builder = new NpgsqlDataSourceBuilder(connectionStringProvider.ConnectionString);

        builder.MapComposite<OutboxUpdateType>("outbox_update_type");

        _npgsqlDataSource = builder.Build();
    }

    protected override string _querySql { get; } =
        """
        SELECT id AS Id, type AS Type, content AS Content
        FROM outbox_messages
        WHERE processed_on_utc IS NULL
        ORDER BY occurred_on_utc
        LIMIT @BatchSize
        FOR UPDATE -- SKIP LOCKED -- If you require parallel processing
        """;

    protected override string _updateSql { get; } =
        """
        UPDATE outbox_messages
        SET processed_on_utc = v.processed_on_utc,
            error = v.error
        FROM (VALUES
            {0}
        ) AS v(id, processed_on_utc, error)
        WHERE outbox_messages.id = v.id::uuid
        """;

    protected override async Task<DbConnection> openConnection(CancellationToken ct = default)
    {
        // var connection = new NpgsqlConnection(_connectionString);
        // await connection.OpenAsync(ct);
        // return connection;

        // This thrown exception due to too many connections for approximately 100,000 records
        return await _npgsqlDataSource.OpenConnectionAsync(ct);
    }
}

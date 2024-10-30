using System.Data.Common;
using Npgsql;
using OutboxProcessorWorker.Database;

namespace OutboxProcessorWorker.Outbox;

public sealed class OutboxProcessorNpgsql(
    IConnectionStringProvider _connectionStringProvider,
    ILogger<OutboxProcessorNpgsql> _logger,
    IMessagePublisher _messagePublisher) : OutboxProcessorBase(_logger, _messagePublisher)
{
    private readonly string _connectionString = _connectionStringProvider.ConnectionString;

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
        var connection = new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(ct);

        return connection;

        // This thrown exception due to too many connections for approximately 100,000 records
        // return await _npgsqlDataSource.OpenConnectionAsync(ct);
    }
}

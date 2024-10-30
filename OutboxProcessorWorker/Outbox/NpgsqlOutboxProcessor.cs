using System.Data.Common;
using Npgsql;
using OutboxProcessorWorker.Database;

namespace OutboxProcessorWorker.Outbox;

public sealed class NpgsqlOutboxProcessor : OutboxProcessorBase
{
    private readonly NpgsqlDataSource _npgsqlDataSource;

    protected override string _querySql { get; set; } =
        """
        SELECT id AS Id, type AS Type, content AS Content
        FROM outbox_messages
        WHERE processed_on_utc IS NULL
        ORDER BY occurred_on_utc
        LIMIT @BatchSize
        FOR UPDATE -- SKIP LOCKED -- If you require parallel processing
        """;

    protected override string _updateSql { get; set; } =
        """
        UPDATE outbox_messages
        SET processed_on_utc = v.processed_on_utc,
            error = v.error
        FROM (VALUES
            {0}
        ) AS v(id, processed_on_utc, error)
        WHERE outbox_messages.id = v.id::uuid
        """;

    public NpgsqlOutboxProcessor(
        IConnectionStringProvider connectionStringProvider,
        ILogger<NpgsqlOutboxProcessor> logger,
        IMessagePublisher messagePublisher) : base(logger, messagePublisher)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionStringProvider.ConnectionString);

        _npgsqlDataSource = dataSourceBuilder.Build();
    }

    protected override async Task<DbConnection> openConnection(CancellationToken ct = default)
    {
        return await _npgsqlDataSource.OpenConnectionAsync(ct);
    }
}

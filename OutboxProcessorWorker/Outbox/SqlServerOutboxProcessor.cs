using System.Data.Common;
using Microsoft.Data.SqlClient;
using OutboxProcessorWorker.Database;

namespace OutboxProcessorWorker.Outbox;

public sealed class SqlServerOutboxProcessor(
    IConnectionStringProvider _connectionStringProvider,
    ILogger<SqlServerOutboxProcessor> _logger,
    IMessagePublisher _messagePublisher) : OutboxProcessorBase(_logger, _messagePublisher)
{
    protected override string _querySql { get; } =
        """
        SELECT TOP (@BatchSize) [Id], [Type], [Content]
        FROM OutboxMessages WITH (ROWLOCK, UPDLOCK) -- Use WITH (..., READPAST), if you require parallel processing
        WHERE [ProcessedOnUtc] IS NULL
        ORDER BY [OccurredOnUtc]
        """;

    protected override string _updateSql { get; } =
        """
        MERGE INTO OutboxMessages AS target
        USING (VALUES
            {0}
        ) AS source ([Id], [ProcessedOnUtc], [Error])
        ON target.[Id] = source.[Id]
        WHEN MATCHED THEN
            UPDATE SET
                target.[ProcessedOnUtc] = source.[ProcessedOnUtc],
                target.[Error]          = source.[Error];
        """;

    protected override async Task<DbConnection> openConnection(CancellationToken ct = default)
    {
        // SqlClient.SqlException (0x80131904): The incoming request has too many parameters.
        // The server supports a maximum of 2100 parameters. Reduce the number of parameters and resend the request.
        _batchSize = 650;

        var connection = new SqlConnection(_connectionStringProvider.ConnectionString);

        await connection.OpenAsync(ct);

        return connection;
    }
}

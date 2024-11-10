using Dapper;
using OutboxProcessorWorker.Database;
using OutboxProcessorWorker.Domain;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;

namespace OutboxProcessorWorker.Outbox;

public sealed class OutboxProcessor_Npgsql_StoredProc(
    IConnectionStringProvider _connectionStringProvider,
    ILogger<OutboxProcessor_Npgsql_StoredProc> _logger,
    IMessagePublisher _messagePublisher) : OutboxProcessor_Npgsql(_connectionStringProvider, _logger, _messagePublisher)
{
    protected override async Task updateOutboxMessages(ConcurrentQueue<OutboxUpdate> updateQueue, DbConnection connection, DbTransaction transaction)
    {
        if (updateQueue.IsEmpty)
        {
            return;
        }

        List<OutboxUpdateType> outboxUpdateTypes = updateQueue.Select(ou => new OutboxUpdateType(ou.Id, ou.Error)).ToList();

        var parameters = new { update_data = outboxUpdateTypes };

        await connection.ExecuteAsync("update_outbox_messages", parameters, transaction, commandType: CommandType.StoredProcedure);
    }
}

public sealed class OutboxUpdateType(Guid _id, string? _error)
{
    public Guid    Id    { get; init; } = _id;
    public string? Error { get; init; } = _error;
}

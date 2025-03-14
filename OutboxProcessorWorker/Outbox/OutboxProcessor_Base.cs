using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Dapper;
using OutboxProcessorWorker.Domain;

namespace OutboxProcessorWorker.Outbox;

public abstract class OutboxProcessor_Base(ILogger<OutboxProcessor_Base> _logger, IMessagePublisher _messagePublisher) : IOutboxProcessor
{
    protected virtual int _batchSize => 1_000;

    private static readonly ConcurrentDictionary<string, Type> _typeCache = new();

    protected abstract string _querySql { get; }
    protected abstract string _updateSql { get; }

    protected abstract Task<DbConnection> openConnection(CancellationToken ct = default);

    public async Task<int> Execute(CancellationToken ct = default)
    {
        long totalStartingTimestamp = Stopwatch.GetTimestamp();

        await using DbConnection  connection  = await openConnection(ct);
        await using DbTransaction transaction = await connection.BeginTransactionAsync(ct);

        long stepStartingTimestamp = Stopwatch.GetTimestamp();

        List<OutboxMessage> messages = (await connection.QueryAsync<OutboxMessage>(
            _querySql,
            param: new { BatchSize = _batchSize },
            transaction: transaction)).AsList();

        double queryTime = Stopwatch.GetElapsedTime(stepStartingTimestamp).TotalMilliseconds;

        ConcurrentQueue<OutboxUpdate> updateQueue = [];

        stepStartingTimestamp = Stopwatch.GetTimestamp();

        Task[] publishTasks = messages
            .Select(message => publishMessage(message, updateQueue, _messagePublisher))
            .ToArray();

        await Task.WhenAll(publishTasks);

        double publishTime = Stopwatch.GetElapsedTime(stepStartingTimestamp).TotalMilliseconds;

        stepStartingTimestamp = Stopwatch.GetTimestamp();

        await updateOutboxMessages(updateQueue, connection, transaction);

        double updateTime = Stopwatch.GetElapsedTime(stepStartingTimestamp).TotalMilliseconds;

        await transaction.CommitAsync(ct);

        double totalTime = Stopwatch.GetElapsedTime(totalStartingTimestamp).TotalMilliseconds;

        OutboxLoggers.LogProcessingPerformance(_logger, totalTime, queryTime, publishTime, updateTime, messages.Count);

        return messages.Count;
    }

    protected virtual async Task updateOutboxMessages(ConcurrentQueue<OutboxUpdate> updateQueue, DbConnection connection, DbTransaction transaction)
    {
        if (updateQueue.IsEmpty)
        {
            return;
        }

        List<OutboxUpdate> updates = updateQueue.ToList();

        string valuesList = string.Join(",", updateQueue.Select((_, i) => $"(@Id{i}, @ProcessedOn{i}, @Error{i})"));

        var parameters = new DynamicParameters();

        for (int i = 0; i < updateQueue.Count; i++)
        {
            parameters.Add($"Id{i}",          updates[i].Id.ToString());
            parameters.Add($"ProcessedOn{i}", updates[i].ProcessedOnUtc);
            parameters.Add($"Error{i}",       updates[i].Error);
        }

        string formattedSql = string.Format(_updateSql, valuesList);

        await connection.ExecuteAsync(formattedSql, parameters, transaction: transaction);
    }

    private static async Task publishMessage(OutboxMessage message, ConcurrentQueue<OutboxUpdate> updateQueue, IMessagePublisher messagePublisher)
    {
        try
        {
            Type messageType = getOrAddMessageType(message.Type);

            object deserializedMessage = JsonSerializer.Deserialize(message.Content, messageType)!;

            await messagePublisher.Publish(deserializedMessage);

            updateQueue.Enqueue(OutboxUpdate.CreateNew(message.Id));
        }
        catch (Exception ex)
        {
            updateQueue.Enqueue(OutboxUpdate.CreateNew(message.Id, ex.ToString()));
        }
    }

    private static Type getOrAddMessageType(string typename)
    {
        return _typeCache.GetOrAdd(typename, name => _assembly.GetType(name)!);
    }

    private static readonly Assembly _assembly = typeof(OutboxProcessor_Base).Assembly;
}

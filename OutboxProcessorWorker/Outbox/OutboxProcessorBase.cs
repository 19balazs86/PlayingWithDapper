using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using Dapper;
using OutboxProcessorWorker.Domain;

namespace OutboxProcessorWorker.Outbox;

public abstract class OutboxProcessorBase(ILogger<OutboxProcessorBase> _logger, IMessagePublisher _messagePublisher) : IOutboxProcessor
{
    private const int _batchSize = 1_000;

    private static readonly ConcurrentDictionary<string, Type> _typeCache = new();

    protected abstract string _querySql { get; set; }
    protected abstract string _updateSql { get; set; }

    protected abstract Task<DbConnection> openConnection(CancellationToken ct = default);

    public async Task<int> Execute(CancellationToken cancellationToken = default)
    {
        long totalStartingTimestamp = Stopwatch.GetTimestamp();

        await using DbConnection  connection  = await openConnection(cancellationToken);
        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

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

        if (!updateQueue.IsEmpty)
        {
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

        double updateTime = Stopwatch.GetElapsedTime(stepStartingTimestamp).TotalMilliseconds;

        await transaction.CommitAsync(cancellationToken);

        double totalTime = Stopwatch.GetElapsedTime(totalStartingTimestamp).TotalMilliseconds;

        OutboxLoggers.LogProcessingPerformance(_logger, totalTime, queryTime, publishTime, updateTime, messages.Count);

        return messages.Count;
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
        return _typeCache.GetOrAdd(typename, name => AssemblyReference.Assembly.GetType(name)!);
    }
}

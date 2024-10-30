namespace OutboxProcessorWorker.Outbox;

public interface IOutboxProcessor
{
    public Task<int> Execute(CancellationToken cancellationToken = default);
}

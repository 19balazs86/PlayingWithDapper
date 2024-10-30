namespace OutboxProcessorWorker.Outbox;

public interface IMessagePublisher
{
    public Task Publish(object message);
}

public sealed class MessagePublisher : IMessagePublisher
{
    public async Task Publish(object message)
    {
        await Task.Delay(50); // Just simulate sending messages
    }
}

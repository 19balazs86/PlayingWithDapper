namespace OutboxProcessorWorker.Domain;

public sealed record OrderCreatedEvent(Guid OrderId)
{
    public static string FullName { get; } = typeof(OrderCreatedEvent).FullName!;

    public static OrderCreatedEvent CreateNew()
    {
        return new OrderCreatedEvent(Guid.NewGuid());
    }
}

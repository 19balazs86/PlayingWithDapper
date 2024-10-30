namespace OutboxProcessorWorker.Domain;

public readonly record struct OutboxUpdate
{
    public Guid     Id             { get; }
    public DateTime ProcessedOnUtc { get; } = DateTime.UtcNow;
    public string?  Error          { get; }

    private OutboxUpdate(Guid id, string? error = null)
    {
        Id    = id;
        Error = error;
    }

    public static OutboxUpdate CreateNew(Guid id, string? error = null)
    {
        return new OutboxUpdate(id, error);
    }
}

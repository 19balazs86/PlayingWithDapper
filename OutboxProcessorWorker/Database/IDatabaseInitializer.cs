namespace OutboxProcessorWorker.Database;

public interface IDatabaseInitializer
{
    public const int BatchSize    = 1_000;
    public const int TotalRecords = 10_000;

    public Task Execute();
}

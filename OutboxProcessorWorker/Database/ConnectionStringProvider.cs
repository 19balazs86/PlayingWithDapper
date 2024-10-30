namespace OutboxProcessorWorker.Database;

public interface IConnectionStringProvider
{
    string ConnectionString { get; }
}

public sealed class NpgsqlConnectionStringProvider(IConfiguration _configuration) : IConnectionStringProvider
{
    public string ConnectionString { get; } = _configuration.GetConnectionString("PostgreSQL")
        ?? throw new NullReferenceException("PostgreSQL connection string is missing");
}

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

public sealed class SqlServerConnectionStringProvider(IConfiguration _configuration) : IConnectionStringProvider
{
    public string ConnectionString { get; } = _configuration.GetConnectionString("SqlServer")
        ?? throw new NullReferenceException("SqlServer connection string is missing");
}

namespace DapperWebApi.Database;

public interface IConnectionStringProvider
{
    string ConnectionString { get; }
}

public sealed class ConnectionStringProvider : IConnectionStringProvider
{
    private readonly string _connectionString;

    public string ConnectionString => _connectionString;

    public ConnectionStringProvider(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new NullReferenceException("PostgreSQL connection string is missing");
    }
}

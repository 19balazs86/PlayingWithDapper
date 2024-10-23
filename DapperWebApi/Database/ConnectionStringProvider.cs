namespace DapperWebApi.Database;

public interface IConnectionStringProvider
{
    string ConnectionString { get; }
}

public sealed class ConnectionStringProvider(IConfiguration _configuration) : IConnectionStringProvider
{
    private readonly string _connectionString = _configuration.GetConnectionString("PostgreSQL")
        ?? throw new NullReferenceException("PostgreSQL connection string is missing");

    public string ConnectionString => _connectionString;
}

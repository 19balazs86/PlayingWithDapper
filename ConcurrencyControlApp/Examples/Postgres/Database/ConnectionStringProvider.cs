using ConcurrencyControlApp.Common;

namespace ConcurrencyControlApp.Examples.Postgres.Database;

public sealed class ConnectionStringProvider : IConnectionStringProvider
{
    public string ConnectionString { get; } =
        "Host=localhost;Port=5432;Username=postgres;Password=postgrespw;Database=postgres";
}

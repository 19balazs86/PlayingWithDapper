using ConcurrencyControlApp.Common;

namespace ConcurrencyControlApp.Examples.SqlServer.Database;

public sealed class ConnectionStringProvider : IConnectionStringProvider
{
    public string ConnectionString { get; } =
        "Data Source=.\\SQLEXPRESS;Initial Catalog=TestDB;Integrated Security=True;Encrypt=True;Trust Server Certificate=True";
}

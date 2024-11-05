using ConcurrencyControlApp.Common;
using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace ConcurrencyControlApp.Examples.SqlServer.Database;

public sealed class DbUnitOfWork(IConnectionStringProvider _connectionStringProvider) : DbUnitOfWorkBase(_connectionStringProvider)
{
    protected override async Task<DbConnection> createConnection()
    {
        var connection = new SqlConnection(_connectionString);

        await connection.OpenAsync();

        return connection;
    }
}

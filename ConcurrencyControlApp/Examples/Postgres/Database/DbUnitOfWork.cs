using ConcurrencyControlApp.Common;
using Npgsql;
using System.Data.Common;

namespace ConcurrencyControlApp.Examples.Postgres.Database;

public sealed class DbUnitOfWork(IConnectionStringProvider _connectionStringProvider) : DbUnitOfWorkBase(_connectionStringProvider)
{
    protected override async Task<DbConnection> createConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);

        await connection.OpenAsync();

        return connection;
    }
}

using Npgsql;

namespace DapperWebApi.Database;

// This interface is designed for repositories to opening connection and passing the Transaction to Dapper commands
public interface IDatabaseSession : IAsyncDisposable
{
    // You can use System.Data.IDbConnection and IDbTransaction to make it generic for use with other databases, such as MS SQL
    // Since Dapper extends IDbConnection, it will work seamlessly

    Task<NpgsqlConnection> OpenConnection();

    NpgsqlTransaction? Transaction { get; }
}

// This interface is designed for services to initiate a transaction when multiple repositories and steps are involved in completing a task
public interface IDatabaseUnitOfWork
{
    public Task BeginTransaction(CancellationToken ct = default);
    public Task CommitTransaction(CancellationToken ct = default);

    // Note: In the event of an exception, there is no need to worry about rolling back the transaction, as it will be disposed of along with the connection after the request
    public Task RollbackTransaction(CancellationToken ct = default);
}
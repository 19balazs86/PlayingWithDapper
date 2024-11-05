using System.Data.Common;

namespace ConcurrencyControlApp.Common;

// This interface aggregates functionality for managing database connections and transactions
public interface IDbUnitOfWork : IDbConnectionManager, IDbTransactionManager, IAsyncDisposable
{

}

// This interface is designed for repositories to opening connection and passing the Transaction to Dapper commands
public interface IDbConnectionManager
{
    Task<DbConnection> OpenConnection();
    DbTransaction? Transaction { get; }
}

// This interface is designed for services to initiate a transaction when multiple repositories and steps are involved in completing a task
public interface IDbTransactionManager
{
    public Task BeginTransaction(CancellationToken    ct = default);
    public Task CommitTransaction(CancellationToken   ct = default);
    public Task RollbackTransaction(CancellationToken ct = default);
    // Note: In the event of an exception, there is no need to worry about rolling back the transaction, as it will be disposed of along with the connection after the request
}

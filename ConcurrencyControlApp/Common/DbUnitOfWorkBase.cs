using System.Data.Common;

namespace ConcurrencyControlApp.Common;

public abstract class DbUnitOfWorkBase : IDbUnitOfWork
{
    protected readonly string _connectionString;

    // It could simply be an DbConnection, but creating it with a Lazy object ensures thread safety
    private readonly Lazy<Task<DbConnection>> _lazyConnection;

    private bool _isDisposed;

    public DbTransaction? Transaction { get; private set; }

    protected DbUnitOfWorkBase(IConnectionStringProvider connectionStringProvider)
    {
        _connectionString = connectionStringProvider.ConnectionString;

        _lazyConnection = new Lazy<Task<DbConnection>>(createConnection);
    }

    public async Task<DbConnection> OpenConnection()
    {
        return await _lazyConnection.Value;
    }

    public async Task BeginTransaction(CancellationToken ct = default)
    {
        if (Transaction is not null)
        {
            throw new InvalidOperationException("You can begin a transaction only once");
        }

        DbConnection connection = await OpenConnection();

        Transaction = await connection.BeginTransactionAsync(ct);
    }

    public async Task CommitTransaction(CancellationToken ct = default)
    {
        if (Transaction is null)
        {
            throw new InvalidOperationException(_exceptionMessage);
        }

        await Transaction.CommitAsync(ct);
    }

    public async Task RollbackTransaction(CancellationToken ct = default)
    {
        if (Transaction is null)
        {
            throw new InvalidOperationException(_exceptionMessage);
        }

        await Transaction.RollbackAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        // Dispose method will be called multiple times because this class is added to the DI container multiple times
        // BUT it is only created once per scope

        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (Transaction is not null)
        {
            await Transaction.DisposeAsync();
        }

        if (_lazyConnection.IsValueCreated)
        {
            DbConnection connection = await _lazyConnection.Value;

            await connection.DisposeAsync();
        }
    }

    protected abstract Task<DbConnection> createConnection();

    private const string _exceptionMessage = "The transaction is null and was not created using the BeginTransaction method";
}

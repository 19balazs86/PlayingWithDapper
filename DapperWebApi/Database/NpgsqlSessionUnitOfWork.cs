using Npgsql;

namespace DapperWebApi.Database;

public sealed class NpgsqlSessionUnitOfWork : IDatabaseSession, IDatabaseUnitOfWork
{
    private readonly string _connectionString;

    // It could simply be an NpgsqlConnection, but creating it with a Lazy object ensures thread safety
    private readonly Lazy<Task<NpgsqlConnection>> _lazyConnection;

    private bool _isDisposed;

    public NpgsqlTransaction? Transaction { get; private set; }

    public NpgsqlSessionUnitOfWork(IConnectionStringProvider connectionStringProvider)
    {
        _connectionString = connectionStringProvider.ConnectionString;

        _lazyConnection = new Lazy<Task<NpgsqlConnection>>(createConnection);
    }

    public async Task<NpgsqlConnection> OpenConnection()
    {
        return await _lazyConnection.Value;
    }

    public async Task BeginTransaction(CancellationToken ct = default)
    {
        if (Transaction is not null)
        {
            throw new InvalidOperationException("You can begin a transaction only once");
        }

        NpgsqlConnection connection = await OpenConnection();

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
            NpgsqlConnection connection = await _lazyConnection.Value;

            await connection.DisposeAsync();
        }
    }

    private async Task<NpgsqlConnection> createConnection()
    {
        //var dataSourceBuilder       = new NpgsqlDataSourceBuilder(_connectionString);
        //NpgsqlDataSource dataSource = dataSourceBuilder.Build();
        //NpgsqlConnection connection = await dataSource.OpenConnectionAsync();

        var connection = new NpgsqlConnection(_connectionString);

        await connection.OpenAsync();

        return connection;
    }

    private const string _exceptionMessage = "The transaction is null and was not created using the BeginTransaction method";
}

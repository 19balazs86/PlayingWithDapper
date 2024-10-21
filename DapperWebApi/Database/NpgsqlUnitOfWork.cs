// The IDatabaseSession.Transaction property has a private setter, so this class cannot manage transaction operations
// The NpgsqlSessionUnitOfWork class is responsible for handling both IDatabaseSession and IDatabaseUnitOfWork

//using Npgsql;

//namespace DapperWebApi.Database;

//public sealed class NpgsqlUnitOfWork(IDatabaseSession _dbSession) : IDatabaseUnitOfWork
//{
//    private const string _exceptionMessage = "The transaction is null and was not created using the BeginTransaction method";

//    public async Task BeginTransaction(CancellationToken ct = default)
//    {
//        NpgsqlConnection connection = await _dbSession.OpenConnection();

//        _dbSession.Transaction = await connection.BeginTransactionAsync(ct);
//    }

//    public async Task CommitTransaction(CancellationToken ct = default)
//    {
//        if (_dbSession.Transaction is null)
//        {
//            throw new InvalidOperationException(_exceptionMessage);
//        }

//        await _dbSession.Transaction.CommitAsync(ct);
//    }

//    public async Task RollbackTransaction(CancellationToken ct = default)
//    {
//        if (_dbSession.Transaction is null)
//        {
//            throw new InvalidOperationException(_exceptionMessage);
//        }

//        await _dbSession.Transaction.RollbackAsync(ct);
//    }
//}

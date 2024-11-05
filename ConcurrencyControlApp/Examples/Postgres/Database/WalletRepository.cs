using ConcurrencyControlApp.Common;
using Dapper;
using System.Data.Common;

namespace ConcurrencyControlApp.Examples.Postgres.Database;

public interface IWalletRepository
{
    public Task<Wallet> Create(string name, decimal initialBalance);

    public Task<bool> Update(Wallet wallet);

    public Task EnsureTableCreated();
}

public sealed class WalletRepository(IDbConnectionManager _dbConnection) : IWalletRepository
{
    public async Task<Wallet> Create(string name, decimal initialBalance)
    {
        // The xmin is a Postgres built-in system column. It tracks the transaction ID of the creation or the last update.
        // Since xmin is automatically maintained by Postgres, you can use it to check if a row has been modified since it was read.

        const string sql =
            """
            INSERT INTO wallets (name, balance)
            VALUES (@name, @initialBalance)
            RETURNING *, xmin AS RowVersion;
            """;

        var param = new { name, initialBalance };

        DbConnection connection = await _dbConnection.OpenConnection();

        return await connection.QuerySingleAsync<Wallet>(sql, param, transaction: _dbConnection.Transaction);
    }

    public async Task<bool> Update(Wallet wallet)
    {
        // Using this concept, skip the row currently locked by other transactions
        const string sql =
            """
            WITH locked_wallet AS (
                SELECT id
                FROM wallets
                WHERE id = @Id AND xmin = @RowVersion
                FOR UPDATE SKIP LOCKED
            )
            UPDATE wallets
            SET name = @Name, balance = @Balance
            FROM locked_wallet
            WHERE wallets.id = locked_wallet.id;
            """;

        DbConnection connection = await _dbConnection.OpenConnection();

        int numberOfRowsAffected = await connection.ExecuteAsync(sql, param: wallet, transaction: _dbConnection.Transaction);

        return numberOfRowsAffected == 1;
    }

    public async Task EnsureTableCreated()
    {
        const string sql =
            """
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'public' AND tablename = 'wallets') THEN
                    CREATE TABLE wallets (
                        id SERIAL PRIMARY KEY,
                        name VARCHAR(50) NOT NULL,
                        balance DECIMAL(18, 2) NOT NULL
                    );
                END IF;
            END $$;
            """;

        DbConnection connection = await _dbConnection.OpenConnection();

        await connection.ExecuteAsync(sql);
    }
}

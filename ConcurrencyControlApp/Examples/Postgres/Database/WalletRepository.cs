using ConcurrencyControlApp.Common;
using Dapper;
using System.Data.Common;

namespace ConcurrencyControlApp.Examples.Postgres.Database;

public interface IWalletRepository
{
    public Task<Wallet> Create(string name, decimal initialBalance);

    public Task<bool> Update(Wallet wallet);

    public Task<bool> TransferMoneyBetweenWallets(Wallet fromWallet, Wallet toWallet);

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
        // Using this concept (Common Table Expression CTE), and skip the row if currently locked by other transactions
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

    public async Task<bool> TransferMoneyBetweenWallets(Wallet fromWallet, Wallet toWallet)
    {
        // Better solution is a combination of a Composite Type and a Stored Procedure
        // You can even update a list of wallets, as there is no logic in the SQL script other than the update
        // All in all, there are multiple ways to perform this transfer, but the focus is on using RowVersion
        const string sql =
            """
            CREATE TEMP TABLE wallet_temp_table (id INT, balance DECIMAL(18, 2), row_version INT);

            INSERT INTO wallet_temp_table (id, balance, row_version)
            VALUES 
                (@fromId, @fromBalance, @fromRowVersion),
                (@toId,   @toBalance,   @toRowVersion);
            
            WITH locked_wallet AS (
                SELECT w.id, wtt.balance
                FROM wallets w
                JOIN wallet_temp_table wtt ON w.id = wtt.id AND w.xmin = wtt.row_version
                FOR UPDATE SKIP LOCKED
            )
            UPDATE wallets
                SET balance = locked_wallet.balance
            FROM locked_wallet
            WHERE wallets.id = locked_wallet.id;
            """;

        var param = new
        {
            fromId         = fromWallet.Id,
            fromBalance    = fromWallet.Balance,
            fromRowVersion = fromWallet.RowVersion,
            toId           = toWallet.Id,
            toBalance      = toWallet.Balance,
            toRowVersion   = toWallet.RowVersion
        };

        DbConnection connection = await _dbConnection.OpenConnection();

        int numberOfRowsAffected = await connection.ExecuteAsync(sql, param, transaction: _dbConnection.Transaction);

        return numberOfRowsAffected == 4; // The expected number is 4 because there are 2 inserts and 2 updates
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

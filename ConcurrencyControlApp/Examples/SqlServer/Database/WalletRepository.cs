using ConcurrencyControlApp.Common;
using Dapper;
using System.Data.Common;

namespace ConcurrencyControlApp.Examples.SqlServer.Database;

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
        const string sql =
            """
            INSERT INTO [Wallets] ([Name], [Balance])
            OUTPUT inserted.*
            VALUES (@name, @initialBalance);
            """;

        var param = new { name, initialBalance };

        DbConnection connection = await _dbConnection.OpenConnection();

        return await connection.QuerySingleAsync<Wallet>(sql, param, transaction: _dbConnection.Transaction);
    }

    public async Task<bool> Update(Wallet wallet)
    {
        const string sql =
            """
            UPDATE [dbo].[Wallets] WITH (READPAST) -- Skip over rows that are currently locked by other transactions
               SET [Name] = @Name, [Balance] = @Balance
            WHERE [Id] = @Id AND [RowVersion] = @RowVersion
            """;

        DbConnection connection = await _dbConnection.OpenConnection();

        int numberOfRowsAffected = await connection.ExecuteAsync(sql, param: wallet, transaction: _dbConnection.Transaction);

        return numberOfRowsAffected == 1;
    }

    public async Task EnsureTableCreated()
    {
        // ROWVERSION is an SQL Server data type.
        // Provides a unique binary value for each row in a table, automatically updated with every modification
        // It is used for tracking changes and managing concurrency

        const string sql =
            """
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Wallets')
            BEGIN
                CREATE TABLE Wallets (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(50) NOT NULL,
                    Balance DECIMAL(18, 2) NOT NULL,
                    RowVersion ROWVERSION
                );
            END
            """;

        DbConnection connection = await _dbConnection.OpenConnection();

        await connection.ExecuteAsync(sql);
    }
}
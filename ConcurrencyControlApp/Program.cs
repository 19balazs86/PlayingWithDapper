namespace ConcurrencyControlApp;

public static class Program
{
    public static async Task Main(string[] args)
    {
        // Dapper and EF for SQL Server work seamlessly together because they follow the same naming conventions and table structure
        await Examples.SqlServer.Example.Run();
        await Examples.SqlServer.ExampleWithEF.Run();

        await Examples.Postgres.Example.Run();

        // SQL scripts for Dapper using snake_case naming conventions, so they cannot work with EF without alignment
        // Unlike SQL Server, unfortunately, dbContext.Database.EnsureCreated is not compatible with table creation for RowVersion in Postgres
        // Wallet.RowVersion: Use it as uint with EF but as int for Dapper

        // await Examples.Postgres.ExampleWithEF.Run();

        // Generate an initial migration
        // DesignTimeDbContextFactory class is provided for EF tools to use when generating migrations
        // dotnet ef migrations add Initial --context ConcurrencyControlApp.Examples.Postgres.Database.WalletDbContext --output-dir Migrations\Postgres
    }
}

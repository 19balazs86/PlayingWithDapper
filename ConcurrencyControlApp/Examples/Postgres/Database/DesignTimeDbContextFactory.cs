using Microsoft.EntityFrameworkCore.Design;

namespace ConcurrencyControlApp.Examples.Postgres.Database;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<WalletDbContext>
{
    public WalletDbContext CreateDbContext(string[] args)
    {
        return new WalletDbContext(new ConnectionStringProvider());
    }
}

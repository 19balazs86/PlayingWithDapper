using ConcurrencyControlApp.Common;
using Microsoft.EntityFrameworkCore;

namespace ConcurrencyControlApp.Examples.Postgres.Database;

public sealed class WalletDbContext(IConnectionStringProvider _connectionStringProvider) : DbContext
{
    public DbSet<Wallet> Wallets { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(_connectionStringProvider.ConnectionString);

        // options.LogTo(Console.WriteLine, [DbLoggerCategory.Database.Command.Name], Microsoft.Extensions.Logging.LogLevel.Information);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // https://learn.microsoft.com/en-us/ef/core/saving/concurrency?tabs=fluent-api
        // Npgsql: https://www.npgsql.org/efcore/modeling/concurrency.html?tabs=fluent-api

        builder.Entity<Wallet>()
               .Property(w => w.RowVersion)
               .IsRowVersion(); // Database-generated concurrency tokens
        // .IsConcurrencyToken(); // Application-managed concurrency tokens
        // DbUpdateConcurrencyException is thrown if there is a RowVersion mismatch
    }
}

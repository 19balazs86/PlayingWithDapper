using ConcurrencyControlApp.Examples.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace ConcurrencyControlApp.Examples.Postgres;

public static class ExampleWithEF
{
    public static async Task Run()
    {
        await using ServiceProvider serviceProvider = createServiceProvider();

        await ensureTableCreated(serviceProvider);

        Wallet wallet = await createNewWallet(serviceProvider);

        bool isUpdated = await updateWallet(serviceProvider, wallet);
        Debug.Assert(isUpdated);
        isUpdated = await updateWallet(serviceProvider, wallet);
        Debug.Assert(isUpdated); // The second update is also successful due to the EF update of the RowVersion for the entity

        await transferMoneyBetweenWallets(serviceProvider);
    }

    private static async Task<Wallet> createNewWallet(IServiceProvider serviceProvider)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

        var wallet = new Wallet
        {
            Name    = $"Wallet-{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}",
            Balance = 500
        };

        await dbContext.Wallets.AddAsync(wallet);

        await dbContext.SaveChangesAsync();

        return wallet;
    }

    private static async Task<bool> updateWallet(IServiceProvider serviceProvider, Wallet wallet)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

        wallet.Name    =  $"Updated-Wallet-{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}";
        wallet.Balance += Random.Shared.Next(-100, 100);

        EntityEntry<Wallet> entry = dbContext.Wallets.Attach(wallet);

        entry.State = EntityState.Modified;

        // DbUpdateConcurrencyException is thrown if there is a RowVersion mismatch
        int numberOfRowsAffected = await dbContext.SaveChangesAsync();

        return numberOfRowsAffected == 1;
    }

    private static async Task transferMoneyBetweenWallets(ServiceProvider serviceProvider)
    {
        Wallet fromWallet = await createNewWallet(serviceProvider);
        Wallet toWallet   = await createNewWallet(serviceProvider);

        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

        int money = Random.Shared.Next(1, 100);

        fromWallet.Balance += money;
        toWallet.Balance   -= money;

        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();

        dbContext.Wallets.Attach(fromWallet).State = EntityState.Modified;
        dbContext.Wallets.Attach(toWallet).State   = EntityState.Modified;

        // Unlike the solution implemented with Dapper, a deadlock could occur here in a race condition, for instance:
        //
        // Transaction #1
        // UPDATE ... WHERE ID = 1
        // UPDATE ... WHERE ID = 2
        //
        // Transaction #2
        // UPDATE ... WHERE ID = 2
        // UPDATE ... WHERE ID = 1
        //
        // In theory, EF is smart enough to order the updates to prevent this.
        // Using raw SQL might be more Safe. Alternatively, passing a cancellation token with a short wait time could also help.
        int numberOfRowsAffected = await dbContext.SaveChangesAsync();

        if (numberOfRowsAffected == 2)
        {
            await transaction.CommitAsync();
        }
        else
        {
            await transaction.RollbackAsync(); // There is no need to invoke Rollback, as disposing of the transaction achieves the same result
        }

        Debug.Assert(numberOfRowsAffected == 2);
    }

    private static async Task ensureTableCreated(IServiceProvider serviceProvider)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    private static ServiceProvider createServiceProvider()
    {
        var serviceProvider = new ServiceCollection();

        serviceProvider.AddDatabaseInfrastructure();

        return serviceProvider.BuildServiceProvider();
    }
}

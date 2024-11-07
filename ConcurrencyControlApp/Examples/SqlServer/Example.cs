using ConcurrencyControlApp.Common;
using ConcurrencyControlApp.Examples.SqlServer.Database;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace ConcurrencyControlApp.Examples.SqlServer;

public static class Example
{
    public static async Task Run()
    {
        await using ServiceProvider serviceProvider = createServiceProvider();

        await ensureTableCreated(serviceProvider);

        Wallet wallet = await createNewWallet(serviceProvider);

        bool isUpdated = await updateWallet(serviceProvider, wallet); // It should be true

        Debug.Assert(isUpdated);

        isUpdated = await updateWallet(serviceProvider, wallet); // It should be false because, after the update, the wallet.RowVersion remains the same

        Debug.Assert(isUpdated == false);

        await transferMoneyBetweenWallets(serviceProvider);

        await transferMoneyBetweenWalletsV2(serviceProvider);
    }

    private static async Task<Wallet> createNewWallet(IServiceProvider serviceProvider)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        var walletRepository = scope.ServiceProvider.GetRequiredService<IWalletRepository>();

        string walletName = $"Wallet-{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}";

        return await walletRepository.Create(walletName, initialBalance: 500);
    }

    private static async Task<bool> updateWallet(IServiceProvider serviceProvider, Wallet wallet)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        var walletRepository = scope.ServiceProvider.GetRequiredService<IWalletRepository>();

        wallet.Name    =  $"Updated-Wallet-{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}";
        wallet.Balance += Random.Shared.Next(-100, 100);

        return await walletRepository.Update(wallet);
    }

    private static async Task transferMoneyBetweenWallets(ServiceProvider serviceProvider)
    {
        Wallet fromWallet = await createNewWallet(serviceProvider);
        Wallet toWallet   = await createNewWallet(serviceProvider);

        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        var walletRepository   = scope.ServiceProvider.GetRequiredService<IWalletRepository>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<IDbTransactionManager>();

        int money = Random.Shared.Next(1, 100);

        fromWallet.Balance += money;
        toWallet.Balance   -= money;

        await transactionManager.BeginTransaction();

        bool isUpdated = await walletRepository.Update(fromWallet);

        Debug.Assert(isUpdated);

        if (isUpdated)
        {
            // The following update runs in a separate scope and returns immediately without making any changes, due to the READPAST hint and a RowVersion mismatch
            isUpdated = await updateWallet(serviceProvider, fromWallet);
            Debug.Assert(isUpdated == false);

            // The following update runs in a separate scope and makes changes, causing the subsequent update to fail due to a RowVersion mismatch
            // isUpdated = await updateWallet(serviceProvider, toWallet);
            // Debug.Assert(isUpdated);

            isUpdated = await walletRepository.Update(toWallet);

            Debug.Assert(isUpdated);
        }

        if (isUpdated)
        {
            await transactionManager.CommitTransaction();
        }
        else
        {
            await transactionManager.RollbackTransaction();
        }
    }

    private static async Task transferMoneyBetweenWalletsV2(ServiceProvider serviceProvider)
    {
        Wallet fromWallet = await createNewWallet(serviceProvider);
        Wallet toWallet   = await createNewWallet(serviceProvider);

        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        var walletRepository   = scope.ServiceProvider.GetRequiredService<IWalletRepository>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<IDbTransactionManager>();

        int money = Random.Shared.Next(1, 100);

        fromWallet.Balance += money;
        toWallet.Balance   -= money;

        await transactionManager.BeginTransaction();

        bool isUpdated = await walletRepository.TransferMoneyBetweenWallets(fromWallet, toWallet);

        Debug.Assert(isUpdated);

        if (isUpdated)
        {
            await transactionManager.CommitTransaction();
        }
        else
        {
            await transactionManager.RollbackTransaction();
        }
    }

    private static async Task ensureTableCreated(IServiceProvider serviceProvider)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        var walletRepository = scope.ServiceProvider.GetRequiredService<IWalletRepository>();

        await walletRepository.EnsureTableCreated();
    }

    private static ServiceProvider createServiceProvider()
    {
        var serviceProvider = new ServiceCollection();

        serviceProvider.AddDatabaseInfrastructure();

        return serviceProvider.BuildServiceProvider();
    }
}

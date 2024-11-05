using ConcurrencyControlApp.Common;
using ConcurrencyControlApp.Examples.SqlServer.Database;
using Microsoft.Extensions.DependencyInjection;

namespace ConcurrencyControlApp.Examples.SqlServer;

public static class InfrastructureExtension
{
    public static void AddDatabaseInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();

        services.AddScoped<IDbUnitOfWork, DbUnitOfWork>();
        services.AddScoped<IDbConnectionManager>( sp => sp.GetRequiredService<IDbUnitOfWork>());
        services.AddScoped<IDbTransactionManager>(sp => sp.GetRequiredService<IDbUnitOfWork>());

        services.AddScoped<IWalletRepository, WalletRepository>();
    }
}

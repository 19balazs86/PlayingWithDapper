using ConcurrencyControlApp.Common;
using ConcurrencyControlApp.Examples.Postgres.Database;
using Microsoft.Extensions.DependencyInjection;

namespace ConcurrencyControlApp.Examples.Postgres;

public static class InfrastructureExtension
{
    public static void AddDatabaseInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();

        services.AddScoped<IDbUnitOfWork, DbUnitOfWork>();
        services.AddScoped<IDbConnectionManager>( sp => sp.GetRequiredService<IDbUnitOfWork>());
        services.AddScoped<IDbTransactionManager>(sp => sp.GetRequiredService<IDbUnitOfWork>());

        services.AddScoped<IWalletRepository, WalletRepository>();

        services.AddDbContext<WalletDbContext>();
    }
}

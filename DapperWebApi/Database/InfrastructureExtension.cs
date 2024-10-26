using Dapper;

namespace DapperWebApi.Database;

public static class InfrastructureExtension
{
    public static void AddDatabaseInfrastructure(this IServiceCollection services)
    {
        services.AddHostedService<MigrationBackgroundService>();

        services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();

        services.AddScoped<NpgsqlSessionUnitOfWork>();
        services.AddScoped<IDatabaseSession>(   sp => sp.GetRequiredService<NpgsqlSessionUnitOfWork>());
        services.AddScoped<IDatabaseUnitOfWork>(sp => sp.GetRequiredService<NpgsqlSessionUnitOfWork>());

        SqlMapper.AddTypeHandler(new DateOnlySqlTypeHandler());
    }
}

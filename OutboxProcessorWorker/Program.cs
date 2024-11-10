using OutboxProcessorWorker.Database;
using OutboxProcessorWorker.Outbox;

namespace OutboxProcessorWorker;

public static class Program
{
    public static async Task Main(string[] args)
    {
        HostApplicationBuilder builder  = Host.CreateApplicationBuilder(args);
        IServiceCollection     services = builder.Services;

        // Add services to the container
        {
            // services.addOutbox_For_Npgsql();
            services.addOutbox_For_SqlServer();

            services.AddHostedService<OutboxBackgroundWorker>();

            services.AddSingleton<IMessagePublisher, MessagePublisher>();
        }

        IHost host = builder.Build();

        await host.setupDatabase(); // Database setup

        await host.RunAsync();
    }

    private static void addOutbox_For_Npgsql(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionStringProvider, NpgsqlConnectionStringProvider>();
        services.AddSingleton<IDatabaseInitializer,      DatabaseInitializer_Npgsql>();
        // services.AddScoped<IOutboxProcessor,             OutboxProcessor_Npgsql>();
        services.AddScoped<IOutboxProcessor,             OutboxProcessor_Npgsql_StoredProc>();
    }

    private static void addOutbox_For_SqlServer(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionStringProvider, SqlServerConnectionStringProvider>();
        services.AddSingleton<IDatabaseInitializer,      DatabaseInitializer_SqlServer>();
        // services.AddScoped<IOutboxProcessor,             OutboxProcessor_SqlServer>();
        services.AddScoped<IOutboxProcessor,             OutboxProcessor_SqlServer_StoredProc>();
    }

    private static async Task setupDatabase(this IHost host)
    {
        var dbInitializer = host.Services.GetRequiredService<IDatabaseInitializer>();

        await dbInitializer.Execute();
    }
}

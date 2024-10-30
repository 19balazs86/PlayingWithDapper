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
            services.addOutboxForNpgsql();

            services.AddHostedService<OutboxBackgroundWorker>();

            services.AddSingleton<IMessagePublisher, MessagePublisher>();
        }

        IHost host = builder.Build();

        await host.setupDatabase(); // Database setup

        await host.RunAsync();
    }

    private static void addOutboxForNpgsql(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionStringProvider, NpgsqlConnectionStringProvider>();
        services.AddSingleton<IDatabaseInitializer,      NpgsqlDatabaseInitializer>();
        services.AddScoped<IOutboxProcessor,             NpgsqlOutboxProcessor>();
    }

    private static async Task setupDatabase(this IHost host)
    {
        var dbInitializer = host.Services.GetRequiredService<IDatabaseInitializer>();

        await dbInitializer.Execute();
    }
}

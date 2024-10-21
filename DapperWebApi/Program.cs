using DapperWebApi.Database;

namespace DapperWebApi;

public static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        IServiceCollection services   = builder.Services;

        string connectionString = builder.Configuration.GetConnectionString("PostgreSQL")!;

        // Add services to the container
        {
            services.AddHostedService<MigrationBackgroundService>();
        }

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline
        {
            app.MapGet("/", () => "Hello DapperWebApi");
        }

        app.Run();
    }
}

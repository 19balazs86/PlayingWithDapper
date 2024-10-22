using DapperWebApi.Database;
using DapperWebApi.Features;

namespace DapperWebApi;

public static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        IServiceCollection services   = builder.Services;

        // Add services to the container
        {
            services.AddDatabaseInfrastructure();

            services.AddFeatures();

            services.ConfigureHttpJsonOptions(options => options.SerializerOptions.PropertyNamingPolicy = null); // null makes is PascalCase. Default: JsonNamingPolicy.CamelCase;
        }

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline
        {
            app.MapGet("/", () => "Hello DapperWebApi");

            app.MapFeatureEndpoints();
        }

        app.Run();
    }
}

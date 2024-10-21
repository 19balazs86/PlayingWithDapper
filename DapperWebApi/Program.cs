namespace DapperWebApi;

public static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        IServiceCollection services   = builder.Services;
        IConfiguration configuration  = builder.Configuration;

        // Add services to the container
        {

        }

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline
        {
            app.MapGet("/", () => "Hello DapperWebApi");
        }

        app.Run();
    }
}

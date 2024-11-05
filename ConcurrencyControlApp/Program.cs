using ConcurrencyControlApp.Examples.SqlServer;

namespace ConcurrencyControlApp;

public static class Program
{
    public static async Task Main(string[] args)
    {
        await Example.Run();
    }
}

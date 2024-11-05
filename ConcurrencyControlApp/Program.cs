namespace ConcurrencyControlApp;

public static class Program
{
    public static async Task Main(string[] args)
    {
        await Examples.SqlServer.Example.Run();

        await Examples.SqlServer.ExampleWithEF.Run();

        await Examples.Postgres.Example.Run();
    }
}

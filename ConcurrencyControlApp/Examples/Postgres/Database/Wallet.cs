namespace ConcurrencyControlApp.Examples.Postgres.Database;

public sealed class Wallet
{
    public int     Id        { get; set; }
    public string  Name      { get; set; } = string.Empty;
    public decimal Balance   { get; set; }
    public int    RowVersion { get; set; } // Use it as uint with EF | Use it as int for Dapper
}

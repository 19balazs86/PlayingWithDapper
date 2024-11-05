namespace ConcurrencyControlApp.Examples.SqlServer.Database;

public sealed class Wallet
{
    public int     Id         { get; set; }
    public string  Name       { get; set; }
    public decimal Balance    { get; set; }
    public byte[]  RowVersion { get; set; }
}

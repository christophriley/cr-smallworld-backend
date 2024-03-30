namespace CRSmallWorldBackend;

public class Transaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public required DateTime TimeStamp { get; set; }
    public required long Amount { get; set; }
    public long SpentAmount { get; set; } = 0;
    public string? CreditWalletId { get; set; }
    public required string DebitWalletId { get; set; }
}
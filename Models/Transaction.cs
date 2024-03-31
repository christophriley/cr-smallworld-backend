namespace CRSmallWorldBackend.Models;

public class Transaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public required DateTime TimeStamp { get; set; }
    public required long Points { get; set; }
    public long SpentPoints { get; set; } = 0;
    public string? CreditWalletId { get; set; }
    public string? DebitWalletId { get; set; }
}
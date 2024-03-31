namespace CRSmallWorldBackend.Models
{
    public class PointDeduction
    {
        public required string WalletId { get; set; }
        public long Points { get; set; }
    }
}
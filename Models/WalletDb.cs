using Microsoft.EntityFrameworkCore;

namespace CRSmallWorldBackend;
public class WalletDb(DbContextOptions<WalletDb> options) : DbContext(options)
{
    public DbSet<Wallet> Wallets => Set<Wallet>();
}
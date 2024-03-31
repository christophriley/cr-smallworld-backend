using Microsoft.EntityFrameworkCore;

namespace CRSmallWorldBackend.Models;
public class WalletDb(DbContextOptions<WalletDb> options) : DbContext(options)
{
    public DbSet<Wallet> Wallets => Set<Wallet>();
}
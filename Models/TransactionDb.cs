using Microsoft.EntityFrameworkCore;

namespace CRSmallWorldBackend;
public class TransactionDb(DbContextOptions<TransactionDb> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
}
using Microsoft.EntityFrameworkCore;

namespace CRSmallWorldBackend.Models;
public class TransactionDb(DbContextOptions<TransactionDb> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
}
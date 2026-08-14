using Microsoft.EntityFrameworkCore;
using Niuro.Core.Domain.Entities;

namespace Niuro.Core.Infrastructure;

/// <summary>
/// Application DbContext. Unit of work + repository for this solution's trivial CRUD.
/// All transactional writes (Customer + Application + OutboxEvent) are committed
/// with a single SaveChanges.
/// </summary>
public class NiuroDbContext(DbContextOptions<NiuroDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<LoanApplication> Applications => Set<LoanApplication>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<BlacklistedSsn> BlacklistedSsns => Set<BlacklistedSsn>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NiuroDbContext).Assembly);
    }
}
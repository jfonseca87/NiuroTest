using Microsoft.EntityFrameworkCore;
using Niuro.Core.Domain.Entities;

namespace Niuro.Core.Infrastructure;

/// <summary>
/// DbContext de la aplicación. Unit of work + repository para el CRUD trivial de esta solución.
/// Todas las escrituras transaccionales (Customer + Application + OutboxEvent) se commitean
/// con un solo SaveChanges (UC-11/12).
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
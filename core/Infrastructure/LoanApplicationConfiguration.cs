using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Niuro.Core.Domain.Entities;

namespace Niuro.Core.Infrastructure;

public class LoanApplicationConfiguration : IEntityTypeConfiguration<LoanApplication>
{
    public void Configure(EntityTypeBuilder<LoanApplication> builder)
    {
        builder.ToTable("Applications");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.RequestedAmount).HasPrecision(18, 2);

        // Invariant: the same SSN = a single Application (updated, not duplicated).
        builder.HasIndex(a => a.CustomerId).IsUnique();

        builder.HasOne(a => a.Customer)
            .WithOne(c => c.Application)
            .HasForeignKey<LoanApplication>(a => a.CustomerId);
    }
}
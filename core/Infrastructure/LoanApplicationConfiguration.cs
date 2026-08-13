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

        // Invariante UC-12: un mismo SSN = una sola Application (se actualiza, no duplica).
        builder.HasIndex(a => a.CustomerId).IsUnique();

        builder.HasOne(a => a.Customer)
            .WithOne(c => c.Application)
            .HasForeignKey<LoanApplication>(a => a.CustomerId);
    }
}
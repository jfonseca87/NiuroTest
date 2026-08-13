using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Niuro.Core.Domain.Entities;

namespace Niuro.Core.Infrastructure;

public class BlacklistedSsnConfiguration : IEntityTypeConfiguration<BlacklistedSsn>
{
    public void Configure(EntityTypeBuilder<BlacklistedSsn> builder)
    {
        builder.ToTable("BlacklistedSsns");
        builder.HasKey(b => b.Ssn);
        builder.Property(b => b.Ssn).HasMaxLength(11).IsRequired();
    }
}
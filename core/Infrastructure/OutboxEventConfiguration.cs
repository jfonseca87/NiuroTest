using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Niuro.Core.Domain.Entities;

namespace Niuro.Core.Infrastructure;

public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("OutboxEvents");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Operation).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(e => e.Payload).HasColumnType("jsonb");

        // The worker queries by pending status in creation order.
        builder.HasIndex(e => new { e.Status, e.CreatedAt }).HasDatabaseName("IX_OutboxEvents_Status_CreatedAt");
        builder.Property(e => e.CreatedAt).IsRequired();
    }
}
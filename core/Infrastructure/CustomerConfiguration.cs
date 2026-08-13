using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Niuro.Core.Domain.Entities;

namespace Niuro.Core.Infrastructure;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Ssn)
            .HasMaxLength(11)
            .IsRequired();

        // Invariante UC-12: un mismo SSN = un solo Customer (la rama update actualiza, no inserta).
        builder.HasIndex(c => c.Ssn).IsUnique();

        builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.CompanyName).HasMaxLength(150).IsRequired();

        // Value object: columnas propias para evitar tablas adicionales.
        builder.OwnsOne(c => c.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("Address_Street").HasMaxLength(200).IsRequired();
            address.Property(a => a.City).HasColumnName("Address_City").HasMaxLength(100).IsRequired();
            address.Property(a => a.State).HasColumnName("Address_State").HasMaxLength(2).IsRequired();
            address.Property(a => a.ZipCode).HasColumnName("Address_ZipCode").HasMaxLength(12).IsRequired();
        });
    }
}
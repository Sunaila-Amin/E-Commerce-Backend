using ECommerce.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.Persistence.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Street)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(a => a.City)
            .HasMaxLength(120);

        builder.Property(a => a.State)
            .HasMaxLength(120);

        builder.Property(a => a.PostalCode)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.Country)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(a => a.Phone)
            .HasMaxLength(30);

        builder.HasIndex(a => a.UserId);
    }
}

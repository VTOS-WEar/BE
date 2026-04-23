using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class ParentAddressConfiguration : IEntityTypeConfiguration<ParentAddress>
{
    public void Configure(EntityTypeBuilder<ParentAddress> builder)
    {
        builder.ToTable("ParentAddress");

        builder.HasKey(address => address.Id);
        builder.Property(address => address.Id).HasColumnName("ParentAddressID");

        builder.Property(address => address.ParentUserID).IsRequired();
        builder.Property(address => address.Label).IsRequired().HasMaxLength(100);
        builder.Property(address => address.RecipientName).IsRequired().HasMaxLength(200);
        builder.Property(address => address.RecipientPhone).IsRequired().HasMaxLength(20);
        builder.Property(address => address.AddressLine).IsRequired().HasMaxLength(500);
        builder.Property(address => address.IsDefault).IsRequired().HasDefaultValue(false);

        builder.Property(address => address.CreatedAt).IsRequired();
        builder.Property(address => address.CreatedBy).HasMaxLength(255);
        builder.Property(address => address.UpdatedBy).HasMaxLength(255);

        builder.HasOne(address => address.ParentUser)
            .WithMany(user => user.Addresses)
            .HasForeignKey(address => address.ParentUserID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(address => address.ParentUserID);
    }
}

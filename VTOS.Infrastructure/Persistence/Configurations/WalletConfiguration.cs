using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallets");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("WalletID");

        builder.Property(w => w.OwnerID)
            .IsRequired();

        builder.Property(w => w.OwnerType)
            .IsRequired();

        builder.Property(w => w.Balance)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(w => w.BankCode)
            .HasMaxLength(20);

        builder.Property(w => w.BankName)
            .HasMaxLength(255);

        builder.Property(w => w.BankAccountNumber)
            .HasMaxLength(50);

        builder.Property(w => w.BankAccountName)
            .HasMaxLength(255);

        builder.Property(w => w.IsActive)
            .IsRequired();

        builder.Property(w => w.CreatedAt)
            .IsRequired();

        builder.Property(w => w.UpdatedAt)
            .IsRequired();

        // Polymorphic ownership — NO FK constraints (OwnerID points to School OR Provider)
        // Navigation properties are ignored in EF; use manual joins in queries
        builder.Ignore(w => w.School);
        builder.Ignore(w => w.Provider);

        // Indexes — unique per owner
        builder.HasIndex(w => new { w.OwnerID, w.OwnerType }).IsUnique();
    }
}

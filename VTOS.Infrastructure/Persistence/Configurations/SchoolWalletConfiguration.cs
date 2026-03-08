using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class SchoolWalletConfiguration : IEntityTypeConfiguration<SchoolWallet>
{
    public void Configure(EntityTypeBuilder<SchoolWallet> builder)
    {
        builder.ToTable("SchoolWallet");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("WalletID");

        builder.Property(w => w.SchoolID)
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

        // Relationships
        builder.HasOne(w => w.School)
            .WithOne(s => s.Wallet)
            .HasForeignKey<SchoolWallet>(w => w.SchoolID)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(w => w.SchoolID);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class ParentBankAccountConfiguration : IEntityTypeConfiguration<ParentBankAccount>
{
    public void Configure(EntityTypeBuilder<ParentBankAccount> builder)
    {
        builder.ToTable("ParentBankAccount");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("BankAccountID");

        builder.Property(b => b.ParentUserID)
            .IsRequired();

        builder.Property(b => b.BankName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(b => b.BankCode)
            .HasMaxLength(50);

        builder.Property(b => b.AccountNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.AccountHolderName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(b => b.IsVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.CreatedBy)
            .HasMaxLength(255);

        builder.Property(b => b.UpdatedBy)
            .HasMaxLength(255);

        // Relationships
        builder.HasOne(b => b.ParentUser)
            .WithMany(u => u.BankAccounts)
            .HasForeignKey(b => b.ParentUserID)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(b => b.ParentUserID);
    }
}

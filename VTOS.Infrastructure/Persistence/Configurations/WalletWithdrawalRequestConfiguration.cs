using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class WalletWithdrawalRequestConfiguration : IEntityTypeConfiguration<WalletWithdrawalRequest>
{
    public void Configure(EntityTypeBuilder<WalletWithdrawalRequest> builder)
    {
        builder.ToTable("WalletWithdrawalRequest");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("WithdrawalID");

        builder.Property(w => w.WalletID)
            .IsRequired();

        builder.Property(w => w.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(w => w.Status)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(w => w.RequestedAt)
            .IsRequired();

        builder.Property(w => w.ApprovedAt);

        builder.Property(w => w.PaidAt);

        builder.Property(w => w.AdminNote)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(w => w.Wallet)
            .WithMany(sw => sw.WithdrawalRequests)
            .HasForeignKey(w => w.WalletID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

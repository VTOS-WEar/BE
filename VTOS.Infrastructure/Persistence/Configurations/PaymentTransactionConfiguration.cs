using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransaction");

        builder.HasKey(pt => pt.Id);
        builder.Property(pt => pt.Id).HasColumnName("PaymentID");

        builder.Property(pt => pt.OrderID)
            .IsRequired();

        builder.Property(pt => pt.WalletID);

        builder.Property(pt => pt.GatewayType)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(pt => pt.TransactionStatus)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(pt => pt.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(pt => pt.TransactionTimestamp)
            .IsRequired();

        builder.Property(pt => pt.TransactionLog)
            .HasMaxLength(2000);

        // Relationships
        builder.HasOne(pt => pt.Order)
            .WithMany(o => o.PaymentTransactions)
            .HasForeignKey(pt => pt.OrderID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pt => pt.Wallet)
            .WithMany(w => w.PaymentTransactions)
            .HasForeignKey(pt => pt.WalletID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


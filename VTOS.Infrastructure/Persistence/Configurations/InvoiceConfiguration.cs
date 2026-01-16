using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoice");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("InvoiceID");

        builder.Property(i => i.OrderID)
            .IsRequired();

        builder.Property(i => i.IssueDate)
            .IsRequired();

        builder.Property(i => i.InvoiceDataURL)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(i => i.Order)
            .WithMany(o => o.Invoices)
            .HasForeignKey(i => i.OrderID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


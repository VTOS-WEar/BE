using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("Complaints");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.Category)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.RequesterRole)
            .HasMaxLength(40);

        builder.Property(x => x.RequesterName)
            .HasMaxLength(200);

        builder.Property(x => x.RequesterEmail)
            .HasMaxLength(256);

        builder.Property(x => x.Response)
            .HasMaxLength(4000);

        builder.Property(x => x.ProofImageUrls)
            .HasColumnType("text");

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(x => new { x.RequesterUserID, x.CreatedAt });
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasIndex(x => x.SchoolID);
        builder.HasIndex(x => x.ProviderID);

        builder.HasOne(x => x.RequesterUser)
            .WithMany()
            .HasForeignKey(x => x.RequesterUserID)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.School)
            .WithMany()
            .HasForeignKey(x => x.SchoolID)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Provider)
            .WithMany()
            .HasForeignKey(x => x.ProviderID)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderID)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.SemesterPublication)
            .WithMany()
            .HasForeignKey(x => x.SemesterPublicationID)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

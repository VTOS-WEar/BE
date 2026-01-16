using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("Campaign");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("CampaignID");

        builder.Property(c => c.SchoolID)
            .IsRequired();

        builder.Property(c => c.CampaignName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.StartDate)
            .IsRequired();

        builder.Property(c => c.EndDate)
            .IsRequired();

        builder.Property(c => c.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        // Relationships
        builder.HasOne(c => c.School)
            .WithMany(s => s.Campaigns)
            .HasForeignKey(c => c.SchoolID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


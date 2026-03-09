using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    public void Configure(EntityTypeBuilder<School> builder)
    {
        builder.ToTable("School");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("SchoolID");

        builder.Property(s => s.SchoolName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(s => s.LogoURL)
            .HasMaxLength(500);

        builder.Property(s => s.ContactInfo)
            .HasMaxLength(2000);

        builder.Property(s => s.Level)
            .HasMaxLength(50);

        builder.Property(s => s.CatalogID);

        // Relationships
        builder.HasMany(s => s.ChildProfiles)
            .WithOne(cp => cp.School)
            .HasForeignKey(cp => cp.SchoolID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


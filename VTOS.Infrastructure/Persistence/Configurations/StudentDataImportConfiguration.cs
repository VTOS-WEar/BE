using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class StudentDataImportConfiguration : IEntityTypeConfiguration<StudentDataImport>
{
    public void Configure(EntityTypeBuilder<StudentDataImport> builder)
    {
        builder.ToTable("StudentDataImport");

        builder.HasKey(sdi => sdi.Id);
        builder.Property(sdi => sdi.Id).HasColumnName("ImportID");

        builder.Property(sdi => sdi.SchoolID)
            .IsRequired();

        builder.Property(sdi => sdi.StudentCode)
            .IsRequired(false)
            .HasMaxLength(50);

        builder.Property(sdi => sdi.FullName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(sdi => sdi.Class)
            .HasMaxLength(50);

        builder.Property(sdi => sdi.ParentPhone)
            .HasMaxLength(50);

        builder.Property(sdi => sdi.DateOfBirth);

        builder.Property(sdi => sdi.Gender)
            .HasMaxLength(10);

        builder.Property(sdi => sdi.IsRegistered)
            .IsRequired();

        builder.Property(sdi => sdi.MatchedChildID);

        // Relationships
        builder.HasOne(sdi => sdi.School)
            .WithMany(s => s.StudentDataImports)
            .HasForeignKey(sdi => sdi.SchoolID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sdi => sdi.MatchedChildProfile)
            .WithMany(cp => cp.StudentDataImports)
            .HasForeignKey(sdi => sdi.MatchedChildID)
            .OnDelete(DeleteBehavior.SetNull);
    }
}


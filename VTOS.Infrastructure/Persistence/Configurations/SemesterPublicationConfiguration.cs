using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class SemesterPublicationConfiguration : IEntityTypeConfiguration<SemesterPublication>
{
    public void Configure(EntityTypeBuilder<SemesterPublication> builder)
    {
        builder.ToTable("SemesterPublication");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("SemesterPublicationID");

        builder.Property(x => x.SchoolID).IsRequired();

        builder.Property(x => x.Semester)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.AcademicYear)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate).IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Rules).HasMaxLength(4000);

        builder.HasIndex(x => new { x.SchoolID, x.Semester, x.AcademicYear })
            .IsUnique();

        builder.HasOne(x => x.School)
            .WithMany(x => x.SemesterPublications)
            .HasForeignKey(x => x.SchoolID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

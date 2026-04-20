using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class ClassGroupConfiguration : IEntityTypeConfiguration<ClassGroup>
{
    public void Configure(EntityTypeBuilder<ClassGroup> builder)
    {
        builder.ToTable("ClassGroups");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClassName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Grade)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.AcademicYear)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.SchoolID, x.ClassName, x.AcademicYear })
            .IsUnique();

        builder.HasOne(x => x.School)
            .WithMany(x => x.ClassGroups)
            .HasForeignKey(x => x.SchoolID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.HomeroomTeacher)
            .WithMany(x => x.HomeroomClasses)
            .HasForeignKey(x => x.HomeroomTeacherID)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

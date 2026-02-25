using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class ChildProfileConfiguration : IEntityTypeConfiguration<ChildProfile>
{
    public void Configure(EntityTypeBuilder<ChildProfile> builder)
    {
        builder.ToTable("Children");

        builder.HasKey(cp => cp.Id);
        builder.Property(cp => cp.Id).HasColumnName("ChildID");

        // ParentUserID is nullable — imported students don't have a linked parent yet
        builder.Property(cp => cp.ParentUserID)
            .IsRequired(false);

        builder.Property(cp => cp.FullName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(cp => cp.Age);

        builder.Property(cp => cp.Grade)
            .HasMaxLength(50);

        builder.Property(cp => cp.Gender)
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(cp => cp.DOB)
            .HasColumnType("date");

        builder.Property(cp => cp.Avatar)
            .HasMaxLength(500);

        builder.Property(cp => cp.SchoolID);

        builder.Property(cp => cp.IsDeleted)
            .IsRequired();

        builder.HasIndex(cp => cp.IsDeleted);

        // ParentUser is optional (null for school-imported students)
        builder.HasOne(cp => cp.ParentUser)
            .WithMany(u => u.ChildProfiles)
            .HasForeignKey(cp => cp.ParentUserID)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cp => cp.School)
            .WithMany(s => s.ChildProfiles)
            .HasForeignKey(cp => cp.SchoolID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

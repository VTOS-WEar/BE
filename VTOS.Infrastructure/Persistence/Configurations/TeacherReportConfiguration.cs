using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class TeacherReportConfiguration : IEntityTypeConfiguration<TeacherReport>
{
    public void Configure(EntityTypeBuilder<TeacherReport> builder)
    {
        builder.ToTable("TeacherReports");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.ReportType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ReviewNote)
            .HasMaxLength(1000);

        builder.Property(x => x.SubmittedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.TeacherUserID, x.Status });
        builder.HasIndex(x => new { x.ClassGroupID, x.SubmittedAt });

        builder.HasOne(x => x.ClassGroup)
            .WithMany()
            .HasForeignKey(x => x.ClassGroupID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TeacherUser)
            .WithMany()
            .HasForeignKey(x => x.TeacherUserID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

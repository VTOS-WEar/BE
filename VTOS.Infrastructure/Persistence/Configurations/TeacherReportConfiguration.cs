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

        builder.Property(x => x.ReviewNote)
            .HasMaxLength(2000);

        builder.Property(x => x.ReportType)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.SubmittedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.TeacherUserId, x.ClassGroupId, x.SubmittedAt });
        builder.HasIndex(x => new { x.ClassGroupId, x.Status });

        builder.HasOne(x => x.ClassGroup)
            .WithMany()
            .HasForeignKey(x => x.ClassGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TeacherUser)
            .WithMany()
            .HasForeignKey(x => x.TeacherUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

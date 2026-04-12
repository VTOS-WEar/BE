using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class BodygramScanLogConfiguration : IEntityTypeConfiguration<BodygramScanLog>
{
    public void Configure(EntityTypeBuilder<BodygramScanLog> builder)
    {
        builder.ToTable("BodygramScanLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CustomScanId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.BodygramScanId)
            .HasMaxLength(100);

        builder.Property(x => x.OrganizationId)
            .HasMaxLength(100);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(x => x.Child)
            .WithMany()
            .HasForeignKey(x => x.ChildId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class SchoolManagerConfiguration : IEntityTypeConfiguration<SchoolManager>
{
    public void Configure(EntityTypeBuilder<SchoolManager> builder)
    {
        builder.ToTable("SchoolManager");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("SchoolManagerID");

        builder.Property(s => s.UserID).IsRequired();
        builder.HasIndex(s => s.UserID).IsUnique();

        builder.Property(s => s.SchoolID).IsRequired();

        builder.HasOne(s => s.School)
            .WithMany()
            .HasForeignKey(s => s.SchoolID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

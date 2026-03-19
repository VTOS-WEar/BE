using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class ParentProfileConfiguration : IEntityTypeConfiguration<ParentProfile>
{
    public void Configure(EntityTypeBuilder<ParentProfile> builder)
    {
        builder.ToTable("ParentProfile");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("ParentProfileID");

        builder.Property(p => p.UserID).IsRequired();
        builder.HasIndex(p => p.UserID).IsUnique();

        builder.Property(p => p.DOB).HasColumnType("date");

        builder.Property(p => p.Gender)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}

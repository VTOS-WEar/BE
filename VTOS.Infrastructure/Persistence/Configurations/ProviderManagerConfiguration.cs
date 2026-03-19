using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class ProviderManagerConfiguration : IEntityTypeConfiguration<ProviderManager>
{
    public void Configure(EntityTypeBuilder<ProviderManager> builder)
    {
        builder.ToTable("ProviderManager");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("ProviderManagerID");

        builder.Property(p => p.UserID).IsRequired();
        builder.HasIndex(p => p.UserID).IsUnique();

        builder.Property(p => p.ProviderID).IsRequired();

        builder.HasOne(p => p.Provider)
            .WithMany()
            .HasForeignKey(p => p.ProviderID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

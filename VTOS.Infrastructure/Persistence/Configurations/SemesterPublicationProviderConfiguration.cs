using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class SemesterPublicationProviderConfiguration : IEntityTypeConfiguration<SemesterPublicationProvider>
{
    public void Configure(EntityTypeBuilder<SemesterPublicationProvider> builder)
    {
        builder.ToTable("SemesterPublicationProvider");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("SemesterPublicationProviderID");

        builder.Property(x => x.SemesterPublicationID).IsRequired();
        builder.Property(x => x.ProviderID).IsRequired();
        builder.Property(x => x.ContractID).IsRequired(false);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(x => x.SuspendReason).HasMaxLength(1000);

        builder.HasIndex(x => new { x.SemesterPublicationID, x.ProviderID })
            .IsUnique();

        builder.HasOne(x => x.SemesterPublication)
            .WithMany(x => x.Providers)
            .HasForeignKey(x => x.SemesterPublicationID)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Provider)
            .WithMany(x => x.SemesterPublicationProviders)
            .HasForeignKey(x => x.ProviderID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Contract)
            .WithMany(x => x.SemesterPublicationProviders)
            .HasForeignKey(x => x.ContractID)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

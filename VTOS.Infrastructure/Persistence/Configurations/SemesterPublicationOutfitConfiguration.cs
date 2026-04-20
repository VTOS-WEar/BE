using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class SemesterPublicationOutfitConfiguration : IEntityTypeConfiguration<SemesterPublicationOutfit>
{
    public void Configure(EntityTypeBuilder<SemesterPublicationOutfit> builder)
    {
        builder.ToTable("SemesterPublicationOutfit");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("SemesterPublicationOutfitID");

        builder.Property(x => x.SemesterPublicationID).IsRequired();
        builder.Property(x => x.OutfitID).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => new { x.SemesterPublicationID, x.OutfitID })
            .IsUnique();

        builder.HasOne(x => x.SemesterPublication)
            .WithMany(x => x.Outfits)
            .HasForeignKey(x => x.SemesterPublicationID)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Outfit)
            .WithMany(x => x.SemesterPublicationOutfits)
            .HasForeignKey(x => x.OutfitID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

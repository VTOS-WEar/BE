using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class OutfitRecommendationConfiguration : IEntityTypeConfiguration<OutfitRecommendation>
{
    public void Configure(EntityTypeBuilder<OutfitRecommendation> builder)
    {
        builder.ToTable("OutfitRecommendation");

        builder.HasKey(or => or.Id);
        builder.Property(or => or.Id).HasColumnName("RecommendationID");

        builder.Property(or => or.UserID)
            .IsRequired();

        builder.Property(or => or.OutfitID)
            .IsRequired();

        builder.Property(or => or.RecommendationScore)
            .IsRequired()
            .HasColumnType("decimal(5,2)");

        builder.Property(or => or.RuleConfigID);

        // Relationships
        builder.HasOne(or => or.User)
            .WithMany(u => u.OutfitRecommendations)
            .HasForeignKey(or => or.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(or => or.Outfit)
            .WithMany(o => o.OutfitRecommendations)
            .HasForeignKey(or => or.OutfitID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


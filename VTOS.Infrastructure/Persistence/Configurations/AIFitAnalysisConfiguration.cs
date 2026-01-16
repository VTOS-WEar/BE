using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class AIFitAnalysisConfiguration : IEntityTypeConfiguration<AIFitAnalysis>
{
    public void Configure(EntityTypeBuilder<AIFitAnalysis> builder)
    {
        builder.ToTable("AIFitAnalysis");

        builder.HasKey(aifa => aifa.Id);
        builder.Property(aifa => aifa.Id).HasColumnName("AnalysisID");

        builder.Property(aifa => aifa.TryOnID)
            .IsRequired();

        builder.Property(aifa => aifa.DetectedBodyProportions)
            .HasMaxLength(500);

        builder.Property(aifa => aifa.SuggestedSize)
            .HasMaxLength(50);

        builder.Property(aifa => aifa.FitScore);

        builder.Property(aifa => aifa.AlgorithmVersion)
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(aifa => aifa.TryOnHistory)
            .WithOne(toh => toh.AIFitAnalysis)
            .HasForeignKey<AIFitAnalysis>(aifa => aifa.TryOnID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


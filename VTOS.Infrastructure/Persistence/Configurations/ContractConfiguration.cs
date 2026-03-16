using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("Contract");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ContractName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
        builder.Property(c => c.RejectionReason).HasMaxLength(500);

        builder.HasOne(c => c.School)
            .WithMany(s => s.Contracts)
            .HasForeignKey(c => c.SchoolID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Provider)
            .WithMany(p => p.Contracts)
            .HasForeignKey(c => c.ProviderID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ContractItemConfiguration : IEntityTypeConfiguration<ContractItem>
{
    public void Configure(EntityTypeBuilder<ContractItem> builder)
    {
        builder.ToTable("ContractItem");
        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.PricePerUnit).HasColumnType("decimal(18,2)");

        builder.HasOne(ci => ci.Contract)
            .WithMany(c => c.ContractItems)
            .HasForeignKey(ci => ci.ContractID)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ci => ci.Outfit)
            .WithMany()
            .HasForeignKey(ci => ci.OutfitID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

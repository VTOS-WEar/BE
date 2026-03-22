using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence.Configurations;

public class AccountRequestConfiguration : IEntityTypeConfiguration<AccountRequest>
{
    public void Configure(EntityTypeBuilder<AccountRequest> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ContactEmail)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ContactPhone)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(1000);

        builder.HasOne(x => x.ProcessedByUser)
            .WithMany()
            .HasForeignKey(x => x.ProcessedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.CreatedUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Type);
        builder.HasIndex(x => x.CreatedAt);
    }
}

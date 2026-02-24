using Microsoft.EntityFrameworkCore;
using VTOS.Domain.Entities;

namespace VTOS.Application.Abstractions;

/// <summary>
/// Abstraction for the database context.
/// Defined in Application layer to support dependency inversion.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<EmailVerification> EmailVerifications { get; }
    DbSet<ChildProfile> ChildProfiles { get; }
    DbSet<Feedback> Feedbacks { get; }
    DbSet<School> Schools { get; }
    DbSet<Category> Categories { get; }
    DbSet<Outfit> Outfits { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<SizeChart> SizeCharts { get; }
    DbSet<SizeChartDetail> SizeChartDetails { get; }
    DbSet<OutfitCategory> OutfitCategories { get; }
    DbSet<TryOnHistory> TryOnHistories { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Campaign> Campaigns { get; }
    DbSet<CampaignOutfit> CampaignOutfits { get; }
    DbSet<Provider> Providers { get; }
    DbSet<ProductionBatch> ProductionBatches { get; }
    DbSet<StudentDataImport> StudentDataImports { get; }

    DbSet<T> Set<T>() where T : class;
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

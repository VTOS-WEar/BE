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
    DbSet<SizeChartMeasurement> SizeChartMeasurements { get; }
    DbSet<OutfitCategory> OutfitCategories { get; }
    DbSet<TryOnHistory> TryOnHistories { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<PaymentTransaction> PaymentTransactions { get; }
    DbSet<Refund> Refunds { get; }
    DbSet<Campaign> Campaigns { get; }
    DbSet<CampaignOutfit> CampaignOutfits { get; }
    DbSet<Provider> Providers { get; }
    DbSet<ProductionBatch> ProductionBatches { get; }
    DbSet<ProductionBatchItem> ProductionBatchItems { get; }
    DbSet<StudentDataImport> StudentDataImports { get; }
    DbSet<ImportBatch> ImportBatches { get; }
    DbSet<Complaint> Complaints { get; }
    DbSet<ParentBankAccount> ParentBankAccounts { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<ContractItem> ContractItems { get; }
    DbSet<DeliveryRecord> DeliveryRecords { get; }
    DbSet<DistributionRecord> DistributionRecords { get; }
    DbSet<DistributionSchedule> DistributionSchedules { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    DbSet<Wallet> Wallets { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<WalletWithdrawalRequest> WalletWithdrawalRequests { get; }
    DbSet<ParentProfile> ParentProfiles { get; }
    DbSet<SchoolManager> SchoolManagers { get; }
    DbSet<ProviderManager> ProviderManagers { get; }
    DbSet<AccountRequest> AccountRequests { get; }
    DbSet<NotificationLog> NotificationLogs { get; }
    DbSet<InAppNotification> InAppNotifications { get; }
    DbSet<BodygramScanLog> BodygramScanLogs { get; }
    DbSet<BodygramScanRecord> BodygramScanRecords { get; }
    DbSet<BodygramMeasurementRecord> BodygramMeasurementRecords { get; }

    DbSet<T> Set<T>() where T : class;
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Domain.Entities;

namespace VTOS.Infrastructure.Persistence;

public class VTOSDbContext : DbContext, IApplicationDbContext
{
    public VTOSDbContext(DbContextOptions<VTOSDbContext> options) : base(options)
    {
    }

    // User & Organization Management
    public DbSet<Role> Roles { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ParentProfile> ParentProfiles { get; set; }
    public DbSet<SchoolManager> SchoolManagers { get; set; }
    public DbSet<ProviderManager> ProviderManagers { get; set; }
    public DbSet<School> Schools { get; set; }
    public DbSet<ClassGroup> ClassGroups { get; set; }
    public DbSet<ChildProfile> ChildProfiles { get; set; }
    public DbSet<EmailVerification> EmailVerifications { get; set; }

    // Outfit & Catalog Management
    public DbSet<Outfit> Outfits { get; set; }
    public DbSet<ProductVariant> ProductVariants { get; set; }
    public DbSet<SizeChart> SizeCharts { get; set; }
    public DbSet<SizeChartDetail> SizeChartDetails { get; set; }
    public DbSet<SizeChartMeasurement> SizeChartMeasurements { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<OutfitCategory> OutfitCategories { get; set; }

    // Core Functional Tables
    public DbSet<TryOnHistory> TryOnHistories { get; set; }
    public DbSet<AIFitAnalysis> AIFitAnalyses { get; set; }
    public DbSet<OutfitRecommendation> OutfitRecommendations { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }

    // Order & Payment Management
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<Refund> Refunds { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<WalletWithdrawalRequest> WalletWithdrawalRequests { get; set; }
    public DbSet<ParentBankAccount> ParentBankAccounts { get; set; }

    // Provider, Campaign & Production
    public DbSet<Provider> Providers { get; set; }
    public DbSet<Campaign> Campaigns { get; set; }
    public DbSet<CampaignOutfit> CampaignOutfits { get; set; }
    public DbSet<SemesterPublication> SemesterPublications { get; set; }
    public DbSet<SemesterPublicationOutfit> SemesterPublicationOutfits { get; set; }
    public DbSet<SemesterPublicationProvider> SemesterPublicationProviders { get; set; }
    public DbSet<StudentDataImport> StudentDataImports { get; set; }
    public DbSet<ImportBatch> ImportBatches { get; set; }
    public DbSet<ProductionBatch> ProductionBatches { get; set; }
    public DbSet<ProductionBatchItem> ProductionBatchItems { get; set; }
    public DbSet<SupportTicket> SupportTickets { get; set; }
    public DbSet<Contract> Contracts { get; set; }
    public DbSet<ContractItem> ContractItems { get; set; }

    // Delivery & Distribution (Phase 4)
    public DbSet<DeliveryRecord> DeliveryRecords { get; set; }
    public DbSet<DistributionRecord> DistributionRecords { get; set; }
    public DbSet<DistributionSchedule> DistributionSchedules { get; set; }

    // Chat (Phase 5)
    public DbSet<ChatMessage> ChatMessages { get; set; }

    // Account Requests (System Improvements - Phase 01)
    public DbSet<AccountRequest> AccountRequests { get; set; }

    // Notification tracking (System Improvements - Phase 02)
    public DbSet<NotificationLog> NotificationLogs { get; set; }

    // In-App Notifications
    public DbSet<InAppNotification> InAppNotifications { get; set; }

    // Bodygram Scans
    public DbSet<BodygramScanLog> BodygramScanLogs { get; set; }
    public DbSet<BodygramScanRecord> BodygramScanRecords { get; set; }
    public DbSet<BodygramMeasurementRecord> BodygramMeasurementRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VTOSDbContext).Assembly);

        // Q4: SupportTicket was renamed from Complaint — keep old table name to avoid migration rename
        modelBuilder.Entity<SupportTicket>().ToTable("Complaints");
    }
}

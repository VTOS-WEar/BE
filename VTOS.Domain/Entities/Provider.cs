using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

/// <summary>
/// Represents a provider/supplier in the system.
/// Maps to the Provider table in the database.
/// </summary>
public class Provider : BaseEntity
{
    public string ProviderName { get; set; } = string.Empty;
    public string? ContactPersonName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsDeleted { get; set; }

    // ── Contract template fields ──────────────────────────────────────────────
    public string? TaxCode { get; set; }
    public string? RepresentativeTitle { get; set; }
    
    // Status
    public ProviderStatus Status { get; set; } = ProviderStatus.Pending;
    
    // Verification fields
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    public string? RejectionReason { get; set; }
    public string? VerificationDocumentUrl { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int TotalCompletedOrders { get; set; }

    // Navigation properties
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
<<<<<<< HEAD
    public ICollection<ProviderRating> ProviderRatings { get; set; } = new List<ProviderRating>();
=======
    public ICollection<ProviderCatalogItem> ProviderCatalogItems { get; set; } = new List<ProviderCatalogItem>();
>>>>>>> 348dab5 (feat(be): add provider catalog and direct order pricing)
    public ICollection<SemesterPublicationProvider> SemesterPublicationProviders { get; set; } = new List<SemesterPublicationProvider>();
    public Wallet? Wallet { get; set; }
}

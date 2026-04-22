using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

/// <summary>
/// Contract between a School and a Provider.
/// In the marketplace flow it acts as a supplier-agreement record for a period,
/// with attached sample outfits for reference.
///
/// Status flow:
///   Pending → PendingSchoolSign (Provider approves) → PendingProviderSign (School signs) → Active (Provider signs)
///   Pending / PendingSchoolSign → Cancelled (School cancels)
///   Pending → Rejected (Provider rejects)
///   Active → InUse (publication/order flow uses this supplier agreement) → Fulfilled / Expired
/// </summary>
public class Contract : BaseEntity
{
    public Guid SchoolID { get; set; }
    public Guid ProviderID { get; set; }
    public string ContractName { get; set; } = string.Empty;

    /// <summary>
    /// Pending | PendingSchoolSign | PendingProviderSign | Active
    /// | InUse | Fulfilled | Rejected | Expired | Cancelled
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>Auto-generated on create: HĐ-{YEAR}-{ShortId}</summary>
    public string ContractNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set when Provider approves (moves to PendingSchoolSign).</summary>
    public DateTime? ApprovedAt { get; set; }

    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>Contract expiration date. Required — protects Provider if School doesn't open a campaign.</summary>
    public DateTime ExpiresAt { get; set; }

    // ── Digital Signing ──────────────────────────────────────────────────────

    /// <summary>Base64-encoded PNG of School's handwritten/uploaded signature.</summary>
    public string? SchoolSignature { get; set; }

    /// <summary>UTC timestamp when School signed.</summary>
    public DateTime? SchoolSignedAt { get; set; }

    /// <summary>Base64-encoded PNG of Provider's handwritten/uploaded signature.</summary>
    public string? ProviderSignature { get; set; }

    /// <summary>UTC timestamp when Provider signed.</summary>
    public DateTime? ProviderSignedAt { get; set; }

    // ── OTP (one-time, reused for both School and Provider signing turns) ────

    /// <summary>Current 6-digit OTP code (plain text for MVP).</summary>
    public string? SigningOTPCode { get; set; }

    /// <summary>UTC expiry of the current OTP (10 minutes).</summary>
    public DateTime? SigningOTPExpiry { get; set; }

    /// <summary>"School" or "Provider" — who this OTP was issued for.</summary>
    public string? SigningOTPFor { get; set; }

    /// <summary>
    /// Relative URL of the generated contract PDF, e.g. "/contracts/{id}.pdf".
    /// Null until at least one party signs.
    /// </summary>
    public string? ContractPdfUrl { get; set; }

    // Navigation
    public School School { get; set; } = null!;
    public Provider Provider { get; set; } = null!;
    public ICollection<ContractItem> ContractItems { get; set; } = new List<ContractItem>();
    public ICollection<SemesterPublicationProvider> SemesterPublicationProviders { get; set; } = new List<SemesterPublicationProvider>();
}


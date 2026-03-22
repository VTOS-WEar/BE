using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

/// <summary>
/// Represents a production complaint submitted by a school against a provider.
/// UC 3.9.11: View Production Complaints (School side).
/// Also used by UC 3.12.x (Complaint & Communication Management).
/// </summary>
public class Complaint : AuditableEntity
{
    public Guid CampaignID { get; set; }
    public Guid? BatchID { get; set; }
    public Guid SchoolID { get; set; }
    public Guid? ProviderID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Open;
    public string? Response { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ProofImageUrls { get; set; }  // JSON array of image URLs

    // Navigation properties
    public Campaign Campaign { get; set; } = null!;
    public ProductionBatch? Batch { get; set; }
    public School School { get; set; } = null!;
    public Provider? Provider { get; set; }
}

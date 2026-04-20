using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

/// <summary>
/// Represents a production complaint submitted by a school against a provider.
/// UC 3.9.11: View Production Complaints (School side).
/// Also used by UC 3.12.x (SupportTicket & Communication Management).
/// </summary>
public class SupportTicket : AuditableEntity
{
    public Guid CampaignID { get; set; }
    public Guid? BatchID { get; set; }
    public Guid SchoolID { get; set; }
    public Guid? ProviderID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;
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

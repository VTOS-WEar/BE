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
    public Guid? OrderID { get; set; }
    public Guid? SchoolID { get; set; }
    public Guid? ProviderID { get; set; }
    public Guid? SemesterPublicationID { get; set; }
    public Guid? RequesterUserID { get; set; }
    public string RequesterRole { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;
    public string? Response { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ProofImageUrls { get; set; }  // JSON array of image URLs

    // Navigation properties
    public Order? Order { get; set; }
    public School? School { get; set; }
    public Provider? Provider { get; set; }
    public SemesterPublication? SemesterPublication { get; set; }
    public User? RequesterUser { get; set; }
}

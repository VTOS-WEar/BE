using VTOS.Domain.Common;

namespace VTOS.Domain.Entities;

public class TeacherReport : AuditableEntity
{
    public Guid ClassGroupID { get; set; }
    public Guid TeacherUserID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ReportType { get; set; } = "General";
    public string Status { get; set; } = "Submitted";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }

    public ClassGroup ClassGroup { get; set; } = null!;
    public User TeacherUser { get; set; } = null!;
}

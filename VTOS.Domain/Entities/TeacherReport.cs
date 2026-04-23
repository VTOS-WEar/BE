using VTOS.Domain.Common;
using VTOS.Domain.Enums;

namespace VTOS.Domain.Entities;

public class TeacherReport : BaseEntity
{
    public Guid ClassGroupId { get; set; }
    public Guid TeacherUserId { get; set; }
    public TeacherReportType ReportType { get; set; } = TeacherReportType.General;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public TeacherReportStatus Status { get; set; } = TeacherReportStatus.Submitted;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }

    public ClassGroup ClassGroup { get; set; } = null!;
    public User TeacherUser { get; set; } = null!;
}

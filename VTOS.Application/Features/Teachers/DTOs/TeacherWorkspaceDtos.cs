namespace VTOS.Application.Features.Teachers.DTOs;

public class TeacherClassAttentionDto
{
    public Guid ClassGroupId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public int MissingParentLinkCount { get; set; }
    public int MissingMeasurementCount { get; set; }
    public int OrderedStudentCount { get; set; }
}

public class TeacherReportListItemDto
{
    public Guid Id { get; set; }
    public Guid ClassGroupId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}

public class TeacherReportListResponseDto
{
    public int TotalCount { get; set; }
    public IReadOnlyList<TeacherReportListItemDto> Items { get; set; } = Array.Empty<TeacherReportListItemDto>();
}

public class TeacherDashboardDto
{
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string TeacherEmail { get; set; } = string.Empty;
    public int TotalClasses { get; set; }
    public int TotalStudents { get; set; }
    public int MissingParentLinkCount { get; set; }
    public int MissingMeasurementCount { get; set; }
    public int PendingReviewReportCount { get; set; }
    public IReadOnlyList<TeacherClassAttentionDto> ClassesNeedingAttention { get; set; } = Array.Empty<TeacherClassAttentionDto>();
    public IReadOnlyList<TeacherReportListItemDto> LatestReports { get; set; } = Array.Empty<TeacherReportListItemDto>();
}

public class TeacherClassOrderCoverageDto
{
    public Guid ClassGroupId { get; set; }
    public int TotalStudents { get; set; }
    public int StudentsWithOrders { get; set; }
    public int StudentsWithoutOrders { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ActiveOrders { get; set; }
    public int ShippedOrders { get; set; }
    public int DeliveredOrders { get; set; }
}

public class TeacherClassFeedbackDto
{
    public Guid FeedbackId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? ProviderName { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime Timestamp { get; set; }
}

public class TeacherClassFeedbackListDto
{
    public Guid ClassGroupId { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalFeedbacks { get; set; }
    public IReadOnlyList<TeacherClassFeedbackDto> Items { get; set; } = Array.Empty<TeacherClassFeedbackDto>();
}

public class SubmitTeacherReportRequestDto
{
    public Guid ClassGroupId { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ReviewTeacherReportRequestDto
{
    public string? ReviewNote { get; set; }
}

public class GetTeacherReportsParamsDto
{
    public Guid? ClassGroupId { get; set; }
    public string? Status { get; set; }
    public string? ReportType { get; set; }
}

public class TeacherReminderCandidateStudentDto
{
    public Guid ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
}

public class TeacherReminderCandidateDto
{
    public Guid ParentUserId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string ParentEmail { get; set; } = string.Empty;
    public string? ParentPhone { get; set; }
    public IReadOnlyList<TeacherReminderCandidateStudentDto> PendingStudents { get; set; } = Array.Empty<TeacherReminderCandidateStudentDto>();
}

public class TeacherReminderCandidatesResponseDto
{
    public Guid ClassGroupId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int TotalPendingParents { get; set; }
    public int TotalPendingStudents { get; set; }
    public IReadOnlyList<TeacherReminderCandidateDto> Items { get; set; } = Array.Empty<TeacherReminderCandidateDto>();
}

public class SendTeacherReminderRequestDto
{
    public Guid ClassGroupId { get; set; }
    public IReadOnlyList<Guid>? ParentUserIds { get; set; }
    public string? Note { get; set; }
}

public class TeacherReminderSendResponseDto
{
    public Guid ClassGroupId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int SentCount { get; set; }
    public IReadOnlyList<Guid> ParentUserIds { get; set; } = Array.Empty<Guid>();
}

using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

public record GetTeacherDashboardQuery(Guid UserId);

public interface IGetTeacherDashboardQueryHandler
{
    Task<Result<TeacherDashboardDto>> HandleAsync(GetTeacherDashboardQuery query, CancellationToken ct = default);
}

public record GetTeacherClassOrderCoverageQuery(Guid UserId, Guid ClassGroupId);

public interface IGetTeacherClassOrderCoverageQueryHandler
{
    Task<Result<TeacherClassOrderCoverageDto>> HandleAsync(GetTeacherClassOrderCoverageQuery query, CancellationToken ct = default);
}

public record GetTeacherClassFeedbackQuery(Guid UserId, Guid ClassGroupId, int Limit = 5);

public interface IGetTeacherClassFeedbackQueryHandler
{
    Task<Result<TeacherClassFeedbackListDto>> HandleAsync(GetTeacherClassFeedbackQuery query, CancellationToken ct = default);
}

public record GetTeacherReportsQuery(Guid UserId, Guid? ClassGroupId = null, string? Status = null, string? ReportType = null);

public interface IGetTeacherReportsQueryHandler
{
    Task<Result<TeacherReportListResponseDto>> HandleAsync(GetTeacherReportsQuery query, CancellationToken ct = default);
}

public record GetSchoolTeacherReportsQuery(Guid UserId, Guid? ClassGroupId = null, string? Status = null);

public interface IGetSchoolTeacherReportsQueryHandler
{
    Task<Result<TeacherReportListResponseDto>> HandleAsync(GetSchoolTeacherReportsQuery query, CancellationToken ct = default);
}

public record GetTeacherReminderCandidatesQuery(Guid UserId, Guid ClassGroupId);

public interface IGetTeacherReminderCandidatesQueryHandler
{
    Task<Result<TeacherReminderCandidatesResponseDto>> HandleAsync(GetTeacherReminderCandidatesQuery query, CancellationToken ct = default);
}

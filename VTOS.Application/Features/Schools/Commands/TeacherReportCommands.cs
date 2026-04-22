using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Schools.Commands;

public record SubmitTeacherReportCommand(Guid UserId, Guid ClassGroupId, string ReportType, string Title, string Content);

public interface ISubmitTeacherReportCommandHandler
{
    Task<Result<TeacherReportListItemDto>> HandleAsync(SubmitTeacherReportCommand command, CancellationToken ct = default);
}

public class SubmitTeacherReportCommandHandler : ISubmitTeacherReportCommandHandler
{
    private readonly IApplicationDbContext _db;

    public SubmitTeacherReportCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<TeacherReportListItemDto>> HandleAsync(SubmitTeacherReportCommand command, CancellationToken ct = default)
    {
        var title = (command.Title ?? string.Empty).Trim();
        var content = (command.Content ?? string.Empty).Trim();
        var reportType = string.IsNullOrWhiteSpace(command.ReportType) ? "General" : command.ReportType.Trim();

        if (title.Length < 5)
            return Result<TeacherReportListItemDto>.Failure("Report title must be at least 5 characters.", "REPORT_TITLE_TOO_SHORT");

        if (content.Length < 10)
            return Result<TeacherReportListItemDto>.Failure("Report content must be at least 10 characters.", "REPORT_CONTENT_TOO_SHORT");

        var classGroup = await _db.ClassGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(cg => cg.Id == command.ClassGroupId && cg.HomeroomTeacherID == command.UserId, ct);

        if (classGroup == null)
            return Result<TeacherReportListItemDto>.Failure("Class group not found for this teacher.", "CLASS_GROUP_NOT_FOUND");

        var report = new TeacherReport
        {
            Id = Guid.NewGuid(),
            ClassGroupID = classGroup.Id,
            TeacherUserID = command.UserId,
            ReportType = reportType,
            Title = title,
            Content = content,
            Status = "Submitted",
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.TeacherReports.Add(report);
        await _db.SaveChangesAsync(ct);

        return Result<TeacherReportListItemDto>.Success(new TeacherReportListItemDto
        {
            Id = report.Id,
            ClassGroupId = classGroup.Id,
            ClassName = classGroup.ClassName,
            ReportType = report.ReportType,
            Title = report.Title,
            Content = report.Content,
            Status = report.Status,
            SubmittedAt = report.SubmittedAt,
            ReviewedAt = report.ReviewedAt,
            ReviewNote = report.ReviewNote,
        });
    }
}

public record ReviewTeacherReportCommand(Guid UserId, Guid ReportId, string? ReviewNote);

public interface IReviewTeacherReportCommandHandler
{
    Task<Result<TeacherReportListItemDto>> HandleAsync(ReviewTeacherReportCommand command, CancellationToken ct = default);
}

public class ReviewTeacherReportCommandHandler : IReviewTeacherReportCommandHandler
{
    private readonly IApplicationDbContext _db;

    public ReviewTeacherReportCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<TeacherReportListItemDto>> HandleAsync(ReviewTeacherReportCommand command, CancellationToken ct = default)
    {
        var schoolId = await _db.SchoolManagers
            .AsNoTracking()
            .Where(sm => sm.UserID == command.UserId)
            .Select(sm => (Guid?)sm.SchoolID)
            .FirstOrDefaultAsync(ct);

        if (!schoolId.HasValue)
            return Result<TeacherReportListItemDto>.Failure("School account is not linked to any school.", "SCHOOL_NOT_LINKED");

        var report = await _db.TeacherReports
            .Include(tr => tr.ClassGroup)
            .FirstOrDefaultAsync(tr => tr.Id == command.ReportId, ct);

        if (report == null || report.ClassGroup.SchoolID != schoolId.Value)
            return Result<TeacherReportListItemDto>.Failure("Teacher report not found.", "TEACHER_REPORT_NOT_FOUND");

        report.Status = "Reviewed";
        report.ReviewNote = string.IsNullOrWhiteSpace(command.ReviewNote) ? null : command.ReviewNote.Trim();
        report.ReviewedAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Result<TeacherReportListItemDto>.Success(new TeacherReportListItemDto
        {
            Id = report.Id,
            ClassGroupId = report.ClassGroupID,
            ClassName = report.ClassGroup.ClassName,
            ReportType = report.ReportType,
            Title = report.Title,
            Content = report.Content,
            Status = report.Status,
            SubmittedAt = report.SubmittedAt,
            ReviewedAt = report.ReviewedAt,
            ReviewNote = report.ReviewNote,
        });
    }
}

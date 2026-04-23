using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Notifications;
using VTOS.Application.Features.Teachers.DTOs;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Teachers.Commands;

public record SubmitTeacherReportCommand(Guid UserId, SubmitTeacherReportRequestDto Request);

public interface ISubmitTeacherReportCommandHandler
{
    Task<Result<TeacherReportListItemDto>> HandleAsync(SubmitTeacherReportCommand command, CancellationToken ct = default);
}

public record ReviewTeacherReportCommand(Guid UserId, Guid ReportId, ReviewTeacherReportRequestDto Request);

public interface IReviewTeacherReportCommandHandler
{
    Task<Result<TeacherReportListItemDto>> HandleAsync(ReviewTeacherReportCommand command, CancellationToken ct = default);
}

public record SendTeacherReminderCommand(Guid UserId, SendTeacherReminderRequestDto Request);

public interface ISendTeacherReminderCommandHandler
{
    Task<Result<TeacherReminderSendResponseDto>> HandleAsync(SendTeacherReminderCommand command, CancellationToken ct = default);
}

public class SubmitTeacherReportCommandHandler : ISubmitTeacherReportCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public SubmitTeacherReportCommandHandler(IApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<Result<TeacherReportListItemDto>> HandleAsync(SubmitTeacherReportCommand command, CancellationToken ct = default)
    {
        var title = command.Request.Title?.Trim() ?? string.Empty;
        var content = command.Request.Content?.Trim() ?? string.Empty;
        if (title.Length < 5)
            return Result<TeacherReportListItemDto>.Failure("Title must be at least 5 characters.", "INVALID_TITLE");
        if (content.Length < 10)
            return Result<TeacherReportListItemDto>.Failure("Content must be at least 10 characters.", "INVALID_CONTENT");
        if (!Enum.TryParse<TeacherReportType>(command.Request.ReportType, true, out var reportType))
            return Result<TeacherReportListItemDto>.Failure("Invalid report type.", "INVALID_REPORT_TYPE");

        var classInfo = await _db.ClassGroups
            .AsNoTracking()
            .Where(x => x.Id == command.Request.ClassGroupId && x.HomeroomTeacherID == command.UserId)
            .Select(x => new { x.Id, x.ClassName, x.SchoolID })
            .FirstOrDefaultAsync(ct);

        if (classInfo == null)
            return Result<TeacherReportListItemDto>.Failure("Class group not found.", "CLASS_GROUP_NOT_FOUND");

        var report = new TeacherReport
        {
            Id = Guid.NewGuid(),
            ClassGroupId = classInfo.Id,
            TeacherUserId = command.UserId,
            ReportType = reportType,
            Title = title,
            Content = content,
            Status = TeacherReportStatus.Submitted,
            SubmittedAt = DateTime.UtcNow,
        };

        _db.Set<TeacherReport>().Add(report);
        await _db.SaveChangesAsync(ct);

        await _notificationService.NotifySchoolAsync(
            classInfo.SchoolID,
            "Báo cáo mới từ giáo viên",
            $"Lớp {classInfo.ClassName} vừa gửi báo cáo: {title}",
            "TeacherReport",
            report.Id,
            "TeacherReport",
            "/school/teacher-reports",
            ct);

        return Result<TeacherReportListItemDto>.Success(new TeacherReportListItemDto
        {
            Id = report.Id,
            ClassGroupId = classInfo.Id,
            ClassName = classInfo.ClassName,
            ReportType = reportType.ToString(),
            Title = report.Title,
            Content = report.Content,
            Status = report.Status.ToString(),
            SubmittedAt = report.SubmittedAt,
            ReviewedAt = report.ReviewedAt,
            ReviewNote = report.ReviewNote,
        });
    }
}

public class ReviewTeacherReportCommandHandler : IReviewTeacherReportCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public ReviewTeacherReportCommandHandler(IApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<Result<TeacherReportListItemDto>> HandleAsync(ReviewTeacherReportCommand command, CancellationToken ct = default)
    {
        var schoolId = await _db.SchoolManagers
            .AsNoTracking()
            .Where(x => x.UserID == command.UserId)
            .Select(x => (Guid?)x.SchoolID)
            .FirstOrDefaultAsync(ct);

        if (!schoolId.HasValue)
            return Result<TeacherReportListItemDto>.Failure("User is not linked to any school.", "SCHOOL_NOT_LINKED");

        var report = await _db.Set<TeacherReport>()
            .Include(x => x.ClassGroup)
            .FirstOrDefaultAsync(x => x.Id == command.ReportId && x.ClassGroup.SchoolID == schoolId.Value, ct);

        if (report == null)
            return Result<TeacherReportListItemDto>.Failure("Teacher report not found.", "TEACHER_REPORT_NOT_FOUND");

        report.Status = TeacherReportStatus.Reviewed;
        report.ReviewedAt = DateTime.UtcNow;
        report.ReviewNote = string.IsNullOrWhiteSpace(command.Request.ReviewNote) ? null : command.Request.ReviewNote.Trim();

        await _db.SaveChangesAsync(ct);

        await _notificationService.CreateAsync(
            report.TeacherUserId,
            "Báo cáo đã được nhà trường xem",
            $"Nhà trường đã phản hồi báo cáo \"{report.Title}\" của lớp {report.ClassGroup.ClassName}.",
            "TeacherReport",
            report.Id,
            "TeacherReport",
            "/teacher/reports",
            ct);

        return Result<TeacherReportListItemDto>.Success(new TeacherReportListItemDto
        {
            Id = report.Id,
            ClassGroupId = report.ClassGroupId,
            ClassName = report.ClassGroup.ClassName,
            ReportType = report.ReportType.ToString(),
            Title = report.Title,
            Content = report.Content,
            Status = report.Status.ToString(),
            SubmittedAt = report.SubmittedAt,
            ReviewedAt = report.ReviewedAt,
            ReviewNote = report.ReviewNote,
        });
    }
}

public class SendTeacherReminderCommandHandler : ISendTeacherReminderCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public SendTeacherReminderCommandHandler(IApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<Result<TeacherReminderSendResponseDto>> HandleAsync(SendTeacherReminderCommand command, CancellationToken ct = default)
    {
        var classInfo = await _db.ClassGroups
            .AsNoTracking()
            .Where(x => x.Id == command.Request.ClassGroupId && x.HomeroomTeacherID == command.UserId)
            .Select(x => new { x.Id, x.ClassName })
            .FirstOrDefaultAsync(ct);

        if (classInfo == null)
            return Result<TeacherReminderSendResponseDto>.Failure("Class group not found.", "CLASS_GROUP_NOT_FOUND");

        var pendingParents = await _db.ChildProfiles
            .AsNoTracking()
            .Where(x => x.ClassGroupID == command.Request.ClassGroupId && !x.IsDeleted && x.ParentUserID != null)
            .Where(x => !x.Orders.Any(o => o.OrderStatus != OrderStatus.Cancelled && o.OrderStatus != OrderStatus.Refunded))
            .GroupBy(x => x.ParentUserID!.Value)
            .Select(g => g.Key)
            .ToListAsync(ct);

        var targetParentIds = (command.Request.ParentUserIds == null || command.Request.ParentUserIds.Count == 0)
            ? pendingParents
            : pendingParents.Intersect(command.Request.ParentUserIds).ToList();

        var teacherName = await _db.Users.AsNoTracking()
            .Where(x => x.Id == command.UserId)
            .Select(x => x.FullName)
            .FirstOrDefaultAsync(ct) ?? "Giáo viên chủ nhiệm";

        var note = string.IsNullOrWhiteSpace(command.Request.Note) ? null : command.Request.Note.Trim();
        foreach (var parentUserId in targetParentIds)
        {
            var message = note == null
                ? $"Giáo viên {teacherName} nhắc phụ huynh hoàn tất đơn đồng phục cho lớp {classInfo.ClassName}."
                : $"Giáo viên {teacherName} nhắc phụ huynh hoàn tất đơn đồng phục cho lớp {classInfo.ClassName}. Ghi chú: {note}";

            await _notificationService.CreateAsync(
                parentUserId,
                "Nhắc hoàn tất đơn đồng phục",
                message,
                "TeacherReminder",
                classInfo.Id,
                "ClassGroup",
                "/my-orders",
                ct);
        }

        return Result<TeacherReminderSendResponseDto>.Success(new TeacherReminderSendResponseDto
        {
            ClassGroupId = classInfo.Id,
            ClassName = classInfo.ClassName,
            SentCount = targetParentIds.Count,
            ParentUserIds = targetParentIds,
        });
    }
}

using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Notifications;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

public record SendTeacherReminderCommand(Guid UserId, Guid ClassGroupId, IReadOnlyList<Guid>? ParentUserIds, string? Note);

public interface ISendTeacherReminderCommandHandler
{
    Task<Result<TeacherReminderSendResponseDto>> HandleAsync(SendTeacherReminderCommand command, CancellationToken ct = default);
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
        var classGroup = await _db.ClassGroups
            .AsNoTracking()
            .Include(cg => cg.School)
            .FirstOrDefaultAsync(cg => cg.Id == command.ClassGroupId && cg.HomeroomTeacherID == command.UserId, ct);

        if (classGroup == null)
            return Result<TeacherReminderSendResponseDto>.Failure("Class group not found for this teacher.", "CLASS_GROUP_NOT_FOUND");

        var teacher = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == command.UserId && !u.IsDeleted, ct);

        if (teacher == null)
            return Result<TeacherReminderSendResponseDto>.Failure("Teacher account not found.", "TEACHER_NOT_FOUND");

        var pendingChildren = await _db.ChildProfiles
            .AsNoTracking()
            .Where(cp =>
                cp.ClassGroupID == command.ClassGroupId &&
                !cp.IsDeleted &&
                cp.ParentUserID != null &&
                !cp.Orders.Any(o =>
                    o.ProviderID != null &&
                    o.SemesterPublicationID != null &&
                    o.OrderStatus != OrderStatus.Cancelled))
            .Select(cp => new
            {
                ParentUserId = cp.ParentUserID!.Value,
                ParentName = cp.ParentUser.FullName ?? cp.ParentUser.Email ?? "Phu huynh",
                ChildName = cp.FullName,
            })
            .ToListAsync(ct);

        var grouped = pendingChildren
            .GroupBy(x => new { x.ParentUserId, x.ParentName })
            .ToDictionary(
                group => group.Key.ParentUserId,
                group => new
                {
                    group.Key.ParentName,
                    ChildNames = group.Select(x => x.ChildName).OrderBy(x => x).ToList(),
                });

        var requestedParentIds = (command.ParentUserIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var targetParentIds = requestedParentIds.Count > 0
            ? requestedParentIds.Where(grouped.ContainsKey).ToList()
            : grouped.Keys.ToList();

        if (targetParentIds.Count == 0)
            return Result<TeacherReminderSendResponseDto>.Failure("No pending parents matched this reminder request.", "NO_PENDING_PARENTS");

        var cleanedNote = string.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim();
        var actionUrl = $"/schools/{classGroup.SchoolID}/catalog";

        foreach (var parentUserId in targetParentIds)
        {
            var parent = grouped[parentUserId];
            var childrenLabel = string.Join(", ", parent.ChildNames);
            var message = $"GVCN lop {classGroup.ClassName} nhac quy phu huynh hoan tat dat dong phuc cho {childrenLabel}.";
            if (!string.IsNullOrWhiteSpace(cleanedNote))
            {
                message = $"{message} Ghi chu: {cleanedNote}";
            }

            await _notificationService.CreateAsync(
                parentUserId,
                $"Nhac dat dong phuc lop {classGroup.ClassName}",
                message,
                "TeacherReminder",
                classGroup.Id,
                "ClassGroup",
                actionUrl,
                ct);
        }

        return Result<TeacherReminderSendResponseDto>.Success(new TeacherReminderSendResponseDto
        {
            ClassGroupId = classGroup.Id,
            ClassName = classGroup.ClassName,
            SentCount = targetParentIds.Count,
            ParentUserIds = targetParentIds,
        });
    }
}

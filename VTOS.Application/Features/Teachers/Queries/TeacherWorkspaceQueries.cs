using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Teachers.DTOs;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Teachers.Queries;

public record GetTeacherDashboardQuery(Guid UserId);

public interface IGetTeacherDashboardQueryHandler
{
    Task<Result<TeacherDashboardDto>> HandleAsync(GetTeacherDashboardQuery query, CancellationToken ct = default);
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

internal static class TeacherWorkspaceQueryHelpers
{
    public static IQueryable<ClassGroup> OwnedTeacherClasses(this IApplicationDbContext db, Guid teacherUserId)
    {
        return db.ClassGroups.AsNoTracking().Where(cg => cg.HomeroomTeacherID == teacherUserId);
    }

    public static IQueryable<TeacherReportListItemDto> ProjectTeacherReports(this IQueryable<TeacherReport> queryable)
    {
        return queryable
            .OrderByDescending(x => x.SubmittedAt)
            .Select(x => new TeacherReportListItemDto
            {
                Id = x.Id,
                ClassGroupId = x.ClassGroupId,
                ClassName = x.ClassGroup.ClassName,
                ReportType = x.ReportType.ToString(),
                Title = x.Title,
                Content = x.Content,
                Status = x.Status.ToString(),
                SubmittedAt = x.SubmittedAt,
                ReviewedAt = x.ReviewedAt,
                ReviewNote = x.ReviewNote,
            });
    }

    public static bool IsActiveOrderStatus(OrderStatus status)
    {
        return status != OrderStatus.Cancelled && status != OrderStatus.Refunded;
    }
}

public class GetTeacherDashboardQueryHandler : IGetTeacherDashboardQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetTeacherDashboardQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<TeacherDashboardDto>> HandleAsync(GetTeacherDashboardQuery query, CancellationToken ct = default)
    {
        var teacher = await _db.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == query.UserId && !x.IsDeleted, ct);

        if (teacher == null || !string.Equals(teacher.Role?.RoleName, "HomeroomTeacher", StringComparison.OrdinalIgnoreCase))
            return Result<TeacherDashboardDto>.Failure("Teacher account not found.", "TEACHER_NOT_FOUND");

        var classData = await _db.OwnedTeacherClasses(query.UserId)
            .Select(cg => new
            {
                cg.Id,
                cg.ClassName,
                cg.AcademicYear,
                StudentCount = cg.Students.Count(s => !s.IsDeleted),
                MissingParentLinkCount = cg.Students.Count(s => !s.IsDeleted && s.ParentUserID == null),
                MissingMeasurementCount = cg.Students.Count(s => !s.IsDeleted && (s.HeightCm <= 0 || s.WeightKg <= 0)),
                OrderedStudentCount = cg.Students.Count(s => !s.IsDeleted && s.Orders.Any(o => o.OrderStatus != OrderStatus.Cancelled && o.OrderStatus != OrderStatus.Refunded)),
            })
            .OrderBy(x => x.ClassName)
            .ToListAsync(ct);

        var latestReports = await _db.Set<TeacherReport>()
            .AsNoTracking()
            .Where(x => x.TeacherUserId == query.UserId)
            .ProjectTeacherReports()
            .Take(5)
            .ToListAsync(ct);

        return Result<TeacherDashboardDto>.Success(new TeacherDashboardDto
        {
            TeacherId = teacher.Id,
            TeacherName = teacher.FullName,
            TeacherEmail = teacher.Email,
            TotalClasses = classData.Count,
            TotalStudents = classData.Sum(x => x.StudentCount),
            MissingParentLinkCount = classData.Sum(x => x.MissingParentLinkCount),
            MissingMeasurementCount = classData.Sum(x => x.MissingMeasurementCount),
            PendingReviewReportCount = await _db.Set<TeacherReport>().AsNoTracking().CountAsync(x => x.TeacherUserId == query.UserId && x.Status == TeacherReportStatus.Submitted, ct),
            ClassesNeedingAttention = classData.Select(x => new TeacherClassAttentionDto
            {
                ClassGroupId = x.Id,
                ClassName = x.ClassName,
                AcademicYear = x.AcademicYear,
                StudentCount = x.StudentCount,
                MissingParentLinkCount = x.MissingParentLinkCount,
                MissingMeasurementCount = x.MissingMeasurementCount,
                OrderedStudentCount = x.OrderedStudentCount,
            }).ToList(),
            LatestReports = latestReports,
        });
    }
}

public class GetTeacherReportsQueryHandler : IGetTeacherReportsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetTeacherReportsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<TeacherReportListResponseDto>> HandleAsync(GetTeacherReportsQuery query, CancellationToken ct = default)
    {
        var teacherExists = await _db.Users.AsNoTracking()
            .Include(x => x.Role)
            .AnyAsync(x => x.Id == query.UserId && !x.IsDeleted && x.Role.RoleName == "HomeroomTeacher", ct);

        if (!teacherExists)
            return Result<TeacherReportListResponseDto>.Failure("Teacher account not found.", "TEACHER_NOT_FOUND");

        var reports = _db.Set<TeacherReport>()
            .AsNoTracking()
            .Where(x => x.TeacherUserId == query.UserId);

        if (query.ClassGroupId.HasValue)
            reports = reports.Where(x => x.ClassGroupId == query.ClassGroupId.Value);

        if (Enum.TryParse<TeacherReportStatus>(query.Status, true, out var status))
            reports = reports.Where(x => x.Status == status);

        if (Enum.TryParse<TeacherReportType>(query.ReportType, true, out var reportType))
            reports = reports.Where(x => x.ReportType == reportType);

        var items = await reports.ProjectTeacherReports().ToListAsync(ct);

        return Result<TeacherReportListResponseDto>.Success(new TeacherReportListResponseDto
        {
            TotalCount = items.Count,
            Items = items,
        });
    }
}

public class GetSchoolTeacherReportsQueryHandler : IGetSchoolTeacherReportsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetSchoolTeacherReportsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<TeacherReportListResponseDto>> HandleAsync(GetSchoolTeacherReportsQuery query, CancellationToken ct = default)
    {
        var schoolId = await _db.SchoolManagers
            .AsNoTracking()
            .Where(x => x.UserID == query.UserId)
            .Select(x => (Guid?)x.SchoolID)
            .FirstOrDefaultAsync(ct);

        if (!schoolId.HasValue)
            return Result<TeacherReportListResponseDto>.Failure("User is not linked to any school.", "SCHOOL_NOT_LINKED");

        var reports = _db.Set<TeacherReport>()
            .AsNoTracking()
            .Where(x => x.ClassGroup.SchoolID == schoolId.Value);

        if (query.ClassGroupId.HasValue)
            reports = reports.Where(x => x.ClassGroupId == query.ClassGroupId.Value);

        if (Enum.TryParse<TeacherReportStatus>(query.Status, true, out var status))
            reports = reports.Where(x => x.Status == status);

        var items = await reports.ProjectTeacherReports().ToListAsync(ct);

        return Result<TeacherReportListResponseDto>.Success(new TeacherReportListResponseDto
        {
            TotalCount = items.Count,
            Items = items,
        });
    }
}

public class GetTeacherReminderCandidatesQueryHandler : IGetTeacherReminderCandidatesQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetTeacherReminderCandidatesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<TeacherReminderCandidatesResponseDto>> HandleAsync(GetTeacherReminderCandidatesQuery query, CancellationToken ct = default)
    {
        var classInfo = await _db.ClassGroups
            .AsNoTracking()
            .Where(x => x.Id == query.ClassGroupId && x.HomeroomTeacherID == query.UserId)
            .Select(x => new { x.Id, x.ClassName })
            .FirstOrDefaultAsync(ct);

        if (classInfo == null)
            return Result<TeacherReminderCandidatesResponseDto>.Failure("Class group not found.", "CLASS_GROUP_NOT_FOUND");

        var students = await _db.ChildProfiles
            .AsNoTracking()
            .Where(x => x.ClassGroupID == query.ClassGroupId && !x.IsDeleted && x.ParentUserID != null)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                ParentUserId = x.ParentUserID!.Value,
                ParentName = x.ParentUser.FullName,
                ParentEmail = x.ParentUser.Email,
                ParentPhone = x.ParentUser.Phone,
                HasOrder = x.Orders.Any(o => o.OrderStatus != OrderStatus.Cancelled && o.OrderStatus != OrderStatus.Refunded),
            })
            .ToListAsync(ct);

        var grouped = students
            .Where(x => !x.HasOrder)
            .GroupBy(x => new { x.ParentUserId, x.ParentName, x.ParentEmail, x.ParentPhone })
            .Select(g => new TeacherReminderCandidateDto
            {
                ParentUserId = g.Key.ParentUserId,
                ParentName = g.Key.ParentName,
                ParentEmail = g.Key.ParentEmail,
                ParentPhone = g.Key.ParentPhone,
                PendingStudents = g.Select(x => new TeacherReminderCandidateStudentDto
                {
                    ChildId = x.Id,
                    ChildName = x.FullName,
                }).OrderBy(x => x.ChildName).ToList(),
            })
            .OrderBy(x => x.ParentName)
            .ToList();

        return Result<TeacherReminderCandidatesResponseDto>.Success(new TeacherReminderCandidatesResponseDto
        {
            ClassGroupId = classInfo.Id,
            ClassName = classInfo.ClassName,
            TotalPendingParents = grouped.Count,
            TotalPendingStudents = grouped.Sum(x => x.PendingStudents.Count),
            Items = grouped,
        });
    }
}

public class GetTeacherClassOrderCoverageQueryHandler : IGetTeacherClassOrderCoverageQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetTeacherClassOrderCoverageQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<TeacherClassOrderCoverageDto>> HandleAsync(GetTeacherClassOrderCoverageQuery query, CancellationToken ct = default)
    {
        var classExists = await _db.ClassGroups.AsNoTracking()
            .AnyAsync(x => x.Id == query.ClassGroupId && x.HomeroomTeacherID == query.UserId, ct);

        if (!classExists)
            return Result<TeacherClassOrderCoverageDto>.Failure("Class group not found.", "CLASS_GROUP_NOT_FOUND");

        var classStudents = _db.ChildProfiles.AsNoTracking()
            .Where(x => x.ClassGroupID == query.ClassGroupId && !x.IsDeleted);

        var totalStudents = await classStudents.CountAsync(ct);
        var studentsWithOrders = await classStudents.CountAsync(x => x.Orders.Any(o => o.OrderStatus != OrderStatus.Cancelled && o.OrderStatus != OrderStatus.Refunded), ct);

        var orders = _db.Orders.AsNoTracking().Where(x => x.ChildProfile.ClassGroupID == query.ClassGroupId);
        var totalOrders = await orders.CountAsync(x => x.OrderStatus != OrderStatus.Cancelled && x.OrderStatus != OrderStatus.Refunded, ct);

        return Result<TeacherClassOrderCoverageDto>.Success(new TeacherClassOrderCoverageDto
        {
            ClassGroupId = query.ClassGroupId,
            TotalStudents = totalStudents,
            StudentsWithOrders = studentsWithOrders,
            StudentsWithoutOrders = Math.Max(totalStudents - studentsWithOrders, 0),
            TotalOrders = totalOrders,
            PendingOrders = await orders.CountAsync(x => x.OrderStatus == OrderStatus.Pending || x.OrderStatus == OrderStatus.Paid, ct),
            ActiveOrders = await orders.CountAsync(x => x.OrderStatus == OrderStatus.Accepted || x.OrderStatus == OrderStatus.Confirmed || x.OrderStatus == OrderStatus.Processed || x.OrderStatus == OrderStatus.InProduction || x.OrderStatus == OrderStatus.ReadyToShip, ct),
            ShippedOrders = await orders.CountAsync(x => x.OrderStatus == OrderStatus.Shipped, ct),
            DeliveredOrders = await orders.CountAsync(x => x.OrderStatus == OrderStatus.Delivered, ct),
        });
    }
}

public class GetTeacherClassFeedbackQueryHandler : IGetTeacherClassFeedbackQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetTeacherClassFeedbackQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<TeacherClassFeedbackListDto>> HandleAsync(GetTeacherClassFeedbackQuery query, CancellationToken ct = default)
    {
        var classExists = await _db.ClassGroups.AsNoTracking()
            .AnyAsync(x => x.Id == query.ClassGroupId && x.HomeroomTeacherID == query.UserId, ct);

        if (!classExists)
            return Result<TeacherClassFeedbackListDto>.Failure("Class group not found.", "CLASS_GROUP_NOT_FOUND");

        var feedbacks = await _db.Feedbacks
            .AsNoTracking()
            .Where(x => x.OrderItem.Order.ChildProfile.ClassGroupID == query.ClassGroupId)
            .OrderByDescending(x => x.Timestamp)
            .Select(x => new TeacherClassFeedbackDto
            {
                FeedbackId = x.Id,
                StudentName = x.OrderItem.Order.ChildProfile.FullName,
                ProviderName = x.OrderItem.Order.Provider != null ? x.OrderItem.Order.Provider.ProviderName : null,
                Rating = x.Rating,
                Comment = x.Comment,
                Timestamp = x.Timestamp,
            })
            .ToListAsync(ct);

        return Result<TeacherClassFeedbackListDto>.Success(new TeacherClassFeedbackListDto
        {
            ClassGroupId = query.ClassGroupId,
            AverageRating = feedbacks.Count > 0 ? Math.Round((decimal)feedbacks.Average(x => x.Rating), 1) : 0m,
            TotalFeedbacks = feedbacks.Count,
            Items = feedbacks.Take(Math.Max(query.Limit, 0)).ToList(),
        });
    }
}

using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Queries;

public class GetTeacherDashboardQueryHandler : IGetTeacherDashboardQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetTeacherDashboardQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<TeacherDashboardDto>> HandleAsync(GetTeacherDashboardQuery query, CancellationToken ct = default)
    {
        var teacher = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == query.UserId && !u.IsDeleted, ct);

        if (teacher == null || !string.Equals(teacher.Role?.RoleName, "HomeroomTeacher", StringComparison.OrdinalIgnoreCase))
            return Result<TeacherDashboardDto>.Failure("Teacher account not found.", "TEACHER_NOT_FOUND");

        var classSummaries = await _db.ClassGroups
            .AsNoTracking()
            .Where(cg => cg.HomeroomTeacherID == query.UserId)
            .Select(cg => new TeacherClassAttentionDto
            {
                ClassGroupId = cg.Id,
                ClassName = cg.ClassName,
                AcademicYear = cg.AcademicYear,
                StudentCount = cg.Students.Count(s => !s.IsDeleted),
                MissingParentLinkCount = cg.Students.Count(s => !s.IsDeleted && s.ParentUserID == null),
                MissingMeasurementCount = cg.Students.Count(s => !s.IsDeleted && !(s.HeightCm > 0 && s.WeightKg > 0)),
                OrderedStudentCount = cg.Students.Count(s =>
                    !s.IsDeleted &&
                    s.Orders.Any(o => o.ProviderID != null && o.SemesterPublicationID != null && o.OrderStatus != OrderStatus.Cancelled)),
            })
            .OrderByDescending(x => x.MissingParentLinkCount + x.MissingMeasurementCount + (x.StudentCount - x.OrderedStudentCount))
            .ThenBy(x => x.ClassName)
            .ToListAsync(ct);

        var latestReports = await _db.TeacherReports
            .AsNoTracking()
            .Where(tr => tr.TeacherUserID == query.UserId)
            .OrderByDescending(tr => tr.SubmittedAt)
            .Take(5)
            .Select(tr => new TeacherReportListItemDto
            {
                Id = tr.Id,
                ClassGroupId = tr.ClassGroupID,
                ClassName = tr.ClassGroup.ClassName,
                ReportType = tr.ReportType,
                Title = tr.Title,
                Content = tr.Content,
                Status = tr.Status,
                SubmittedAt = tr.SubmittedAt,
                ReviewedAt = tr.ReviewedAt,
                ReviewNote = tr.ReviewNote,
            })
            .ToListAsync(ct);

        var pendingReviewCount = await _db.TeacherReports
            .AsNoTracking()
            .CountAsync(tr => tr.TeacherUserID == query.UserId && tr.Status != "Reviewed", ct);

        var totalStudents = classSummaries.Sum(x => x.StudentCount);

        return Result<TeacherDashboardDto>.Success(new TeacherDashboardDto
        {
            TeacherId = teacher.Id,
            TeacherName = teacher.FullName,
            TeacherEmail = teacher.Email,
            TotalClasses = classSummaries.Count,
            TotalStudents = totalStudents,
            MissingParentLinkCount = classSummaries.Sum(x => x.MissingParentLinkCount),
            MissingMeasurementCount = classSummaries.Sum(x => x.MissingMeasurementCount),
            PendingReviewReportCount = pendingReviewCount,
            ClassesNeedingAttention = classSummaries.Take(4).ToList(),
            LatestReports = latestReports,
        });
    }
}

public class GetTeacherClassOrderCoverageQueryHandler : IGetTeacherClassOrderCoverageQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetTeacherClassOrderCoverageQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<TeacherClassOrderCoverageDto>> HandleAsync(GetTeacherClassOrderCoverageQuery query, CancellationToken ct = default)
    {
        var classExists = await _db.ClassGroups
            .AsNoTracking()
            .AnyAsync(cg => cg.Id == query.ClassGroupId && cg.HomeroomTeacherID == query.UserId, ct);

        if (!classExists)
            return Result<TeacherClassOrderCoverageDto>.Failure("Class group not found.", "CLASS_GROUP_NOT_FOUND");

        var students = await _db.ChildProfiles
            .AsNoTracking()
            .Where(cp => cp.ClassGroupID == query.ClassGroupId && !cp.IsDeleted)
            .Select(cp => new
            {
                cp.Id,
                HasOrder = cp.Orders.Any(o => o.ProviderID != null && o.SemesterPublicationID != null && o.OrderStatus != OrderStatus.Cancelled),
            })
            .ToListAsync(ct);

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o =>
                o.ChildProfile.ClassGroupID == query.ClassGroupId &&
                o.ProviderID != null &&
                o.SemesterPublicationID != null &&
                o.OrderStatus != OrderStatus.Cancelled)
            .Select(o => o.OrderStatus)
            .ToListAsync(ct);

        return Result<TeacherClassOrderCoverageDto>.Success(new TeacherClassOrderCoverageDto
        {
            ClassGroupId = query.ClassGroupId,
            TotalStudents = students.Count,
            StudentsWithOrders = students.Count(x => x.HasOrder),
            StudentsWithoutOrders = students.Count(x => !x.HasOrder),
            TotalOrders = orders.Count,
            PendingOrders = orders.Count(x => x == OrderStatus.Pending || x == OrderStatus.Paid),
            ActiveOrders = orders.Count(x => x == OrderStatus.Accepted || x == OrderStatus.InProduction || x == OrderStatus.ReadyToShip),
            ShippedOrders = orders.Count(x => x == OrderStatus.Shipped),
            DeliveredOrders = orders.Count(x => x == OrderStatus.Delivered),
        });
    }
}

public class GetTeacherClassFeedbackQueryHandler : IGetTeacherClassFeedbackQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetTeacherClassFeedbackQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<TeacherClassFeedbackListDto>> HandleAsync(GetTeacherClassFeedbackQuery query, CancellationToken ct = default)
    {
        var classExists = await _db.ClassGroups
            .AsNoTracking()
            .AnyAsync(cg => cg.Id == query.ClassGroupId && cg.HomeroomTeacherID == query.UserId, ct);

        if (!classExists)
            return Result<TeacherClassFeedbackListDto>.Failure("Class group not found.", "CLASS_GROUP_NOT_FOUND");

        var feedbackQuery = _db.Feedbacks
            .AsNoTracking()
            .Where(f =>
                f.OrderItem.Order.ChildProfile.ClassGroupID == query.ClassGroupId &&
                f.OrderItem.Order.ProviderID != null &&
                f.OrderItem.Order.SemesterPublicationID != null);

        var total = await feedbackQuery.CountAsync(ct);
        var average = total == 0
            ? 0m
            : Math.Round((decimal)(await feedbackQuery.AverageAsync(f => f.Rating, ct)), 2);

        var items = await feedbackQuery
            .OrderByDescending(f => f.Timestamp)
            .Take(Math.Max(1, query.Limit))
            .Select(f => new TeacherClassFeedbackDto
            {
                FeedbackId = f.Id,
                StudentName = f.OrderItem.Order.ChildProfile.FullName,
                ProviderName = f.OrderItem.Order.Provider != null ? f.OrderItem.Order.Provider.ProviderName : null,
                Rating = f.Rating,
                Comment = f.Comment,
                Timestamp = f.Timestamp,
            })
            .ToListAsync(ct);

        return Result<TeacherClassFeedbackListDto>.Success(new TeacherClassFeedbackListDto
        {
            ClassGroupId = query.ClassGroupId,
            AverageRating = average,
            TotalFeedbacks = total,
            Items = items,
        });
    }
}

public class GetTeacherReportsQueryHandler : IGetTeacherReportsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetTeacherReportsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<TeacherReportListResponseDto>> HandleAsync(GetTeacherReportsQuery query, CancellationToken ct = default)
    {
        var teacherExists = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .AnyAsync(u => u.Id == query.UserId && !u.IsDeleted && u.Role.RoleName == "HomeroomTeacher", ct);

        if (!teacherExists)
            return Result<TeacherReportListResponseDto>.Failure("Teacher account not found.", "TEACHER_NOT_FOUND");

        var reportQuery = _db.TeacherReports
            .AsNoTracking()
            .Where(tr => tr.TeacherUserID == query.UserId);

        if (query.ClassGroupId.HasValue)
            reportQuery = reportQuery.Where(tr => tr.ClassGroupID == query.ClassGroupId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status))
            reportQuery = reportQuery.Where(tr => tr.Status == query.Status);

        if (!string.IsNullOrWhiteSpace(query.ReportType))
            reportQuery = reportQuery.Where(tr => tr.ReportType == query.ReportType);

        var items = await reportQuery
            .OrderByDescending(tr => tr.SubmittedAt)
            .Select(tr => new TeacherReportListItemDto
            {
                Id = tr.Id,
                ClassGroupId = tr.ClassGroupID,
                ClassName = tr.ClassGroup.ClassName,
                ReportType = tr.ReportType,
                Title = tr.Title,
                Content = tr.Content,
                Status = tr.Status,
                SubmittedAt = tr.SubmittedAt,
                ReviewedAt = tr.ReviewedAt,
                ReviewNote = tr.ReviewNote,
            })
            .ToListAsync(ct);

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
            .Where(sm => sm.UserID == query.UserId)
            .Select(sm => (Guid?)sm.SchoolID)
            .FirstOrDefaultAsync(ct);

        if (!schoolId.HasValue)
            return Result<TeacherReportListResponseDto>.Failure("School account is not linked to any school.", "SCHOOL_NOT_LINKED");

        var reportQuery = _db.TeacherReports
            .AsNoTracking()
            .Where(tr => tr.ClassGroup.SchoolID == schoolId.Value);

        if (query.ClassGroupId.HasValue)
            reportQuery = reportQuery.Where(tr => tr.ClassGroupID == query.ClassGroupId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status))
            reportQuery = reportQuery.Where(tr => tr.Status == query.Status);

        var items = await reportQuery
            .OrderByDescending(tr => tr.SubmittedAt)
            .Select(tr => new TeacherReportListItemDto
            {
                Id = tr.Id,
                ClassGroupId = tr.ClassGroupID,
                ClassName = tr.ClassGroup.ClassName,
                ReportType = tr.ReportType,
                Title = tr.Title,
                Content = tr.Content,
                Status = tr.Status,
                SubmittedAt = tr.SubmittedAt,
                ReviewedAt = tr.ReviewedAt,
                ReviewNote = tr.ReviewNote,
            })
            .ToListAsync(ct);

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
        var classGroup = await _db.ClassGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(cg => cg.Id == query.ClassGroupId && cg.HomeroomTeacherID == query.UserId, ct);

        if (classGroup == null)
            return Result<TeacherReminderCandidatesResponseDto>.Failure("Class group not found for this teacher.", "CLASS_GROUP_NOT_FOUND");

        var pendingChildren = await _db.ChildProfiles
            .AsNoTracking()
            .Where(cp =>
                cp.ClassGroupID == query.ClassGroupId &&
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
                ParentEmail = cp.ParentUser.Email ?? string.Empty,
                ParentPhone = cp.ParentUser.Phone,
                ChildId = cp.Id,
                ChildName = cp.FullName,
            })
            .ToListAsync(ct);

        var grouped = pendingChildren
            .GroupBy(x => new { x.ParentUserId, x.ParentName, x.ParentEmail, x.ParentPhone })
            .Select(group => new TeacherReminderCandidateDto
            {
                ParentUserId = group.Key.ParentUserId,
                ParentName = group.Key.ParentName,
                ParentEmail = group.Key.ParentEmail,
                ParentPhone = group.Key.ParentPhone,
                PendingStudents = group
                    .OrderBy(x => x.ChildName)
                    .Select(x => new TeacherReminderCandidateStudentDto
                    {
                        ChildId = x.ChildId,
                        ChildName = x.ChildName,
                    })
                    .ToList(),
            })
            .OrderBy(x => x.ParentName)
            .ToList();

        return Result<TeacherReminderCandidatesResponseDto>.Success(new TeacherReminderCandidatesResponseDto
        {
            ClassGroupId = classGroup.Id,
            ClassName = classGroup.ClassName,
            TotalPendingParents = grouped.Count,
            TotalPendingStudents = pendingChildren.Count,
            Items = grouped,
        });
    }
}

using Microsoft.EntityFrameworkCore;
using VTOS.Application.Features.Notifications;
using VTOS.Application.Features.Teachers.Commands;
using VTOS.Application.Features.Teachers.DTOs;
using VTOS.Application.Features.Teachers.Queries;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;
using VTOS.Infrastructure.Persistence;

namespace VTOS.Application.Tests.TeacherWorkspace;

public class TeacherWorkspaceHandlersTests
{
    [Fact]
    public async Task SubmitTeacherReport_Rejects_Class_Outside_Teacher_Ownership()
    {
        await using var db = CreateDbContext();
        var ids = await SeedTeacherWorkspaceAsync(db);
        var handler = new SubmitTeacherReportCommandHandler(db, new FakeNotificationService());

        var result = await handler.HandleAsync(new SubmitTeacherReportCommand(
            ids.OtherTeacherId,
            new SubmitTeacherReportRequestDto
            {
                ClassGroupId = ids.ClassId,
                ReportType = "General",
                Title = "Wrong owner",
                Content = "This teacher should not be allowed to submit.",
            }));

        Assert.False(result.IsSuccess);
        Assert.Equal("CLASS_GROUP_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task School_Can_List_And_Review_Only_Its_Own_Teacher_Reports()
    {
        await using var db = CreateDbContext();
        var ids = await SeedTeacherWorkspaceAsync(db);

        db.Set<TeacherReport>().Add(new TeacherReport
        {
            Id = Guid.NewGuid(),
            ClassGroupId = ids.ClassId,
            TeacherUserId = ids.TeacherId,
            ReportType = TeacherReportType.General,
            Title = "Class issue",
            Content = "Pending review content",
            Status = TeacherReportStatus.Submitted,
            SubmittedAt = DateTime.UtcNow,
        });

        db.Set<TeacherReport>().Add(new TeacherReport
        {
            Id = Guid.NewGuid(),
            ClassGroupId = ids.OtherSchoolClassId,
            TeacherUserId = ids.OtherTeacherId,
            ReportType = TeacherReportType.QualityIssue,
            Title = "Other school issue",
            Content = "Should not appear",
            Status = TeacherReportStatus.Submitted,
            SubmittedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();

        var listHandler = new GetSchoolTeacherReportsQueryHandler(db);
        var listResult = await listHandler.HandleAsync(new GetSchoolTeacherReportsQuery(ids.SchoolUserId));

        Assert.True(listResult.IsSuccess);
        Assert.NotNull(listResult.Value);
        Assert.Single(listResult.Value!.Items);

        var reportId = listResult.Value.Items[0].Id;
        var reviewHandler = new ReviewTeacherReportCommandHandler(db, new FakeNotificationService());
        var reviewResult = await reviewHandler.HandleAsync(new ReviewTeacherReportCommand(
            ids.SchoolUserId,
            reportId,
            new ReviewTeacherReportRequestDto { ReviewNote = "Reviewed by school" }));

        Assert.True(reviewResult.IsSuccess);
        Assert.NotNull(reviewResult.Value);
        Assert.Equal("Reviewed", reviewResult.Value!.Status);
        Assert.Equal("Reviewed by school", reviewResult.Value.ReviewNote);
        Assert.NotNull(reviewResult.Value.ReviewedAt);
    }

    private static VTOSDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VTOSDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VTOSDbContext(options);
    }

    private static async Task<SeedIds> SeedTeacherWorkspaceAsync(VTOSDbContext db)
    {
        var schoolRole = new Role { Id = Guid.NewGuid(), RoleName = "School", IsSystemRole = true };
        var teacherRole = new Role { Id = Guid.NewGuid(), RoleName = "HomeroomTeacher", IsSystemRole = true };
        var parentRole = new Role { Id = Guid.NewGuid(), RoleName = "Parent", IsSystemRole = true };

        var schoolUserId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var otherTeacherId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var otherParentId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var otherSchoolClassId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();

        db.Roles.AddRange(schoolRole, teacherRole, parentRole);
        db.Users.AddRange(
            new User { Id = schoolUserId, FullName = "School User", Email = "school@example.com", PasswordHash = "x", RoleID = schoolRole.Id, Role = schoolRole, IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { Id = teacherId, FullName = "Teacher A", Email = "teacher@example.com", PasswordHash = "x", RoleID = teacherRole.Id, Role = teacherRole, IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { Id = otherTeacherId, FullName = "Teacher B", Email = "teacher2@example.com", PasswordHash = "x", RoleID = teacherRole.Id, Role = teacherRole, IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { Id = parentId, FullName = "Parent A", Email = "parent@example.com", PasswordHash = "x", RoleID = parentRole.Id, Role = parentRole, IsActive = true, CreatedAt = DateTime.UtcNow, Phone = "0900000001" },
            new User { Id = otherParentId, FullName = "Parent B", Email = "parent2@example.com", PasswordHash = "x", RoleID = parentRole.Id, Role = parentRole, IsActive = true, CreatedAt = DateTime.UtcNow, Phone = "0900000002" });

        db.Schools.AddRange(
            new School { Id = schoolId, SchoolName = "School A", CreatedAt = DateTime.UtcNow },
            new School { Id = otherSchoolId, SchoolName = "School B", CreatedAt = DateTime.UtcNow });

        db.SchoolManagers.Add(new SchoolManager { Id = Guid.NewGuid(), UserID = schoolUserId, SchoolID = schoolId });

        db.ClassGroups.AddRange(
            new ClassGroup { Id = classId, SchoolID = schoolId, ClassName = "6A1", Grade = "6", AcademicYear = "2025-2026", HomeroomTeacherID = teacherId, CreatedAt = DateTime.UtcNow },
            new ClassGroup { Id = otherSchoolClassId, SchoolID = otherSchoolId, ClassName = "7B1", Grade = "7", AcademicYear = "2025-2026", HomeroomTeacherID = otherTeacherId, CreatedAt = DateTime.UtcNow });

        db.ChildProfiles.AddRange(
            new ChildProfile
            {
                Id = studentId,
                FullName = "Student A",
                Age = 11,
                Grade = "6",
                Gender = Gender.Male,
                SchoolID = schoolId,
                ClassGroupID = classId,
                ParentUserID = parentId,
                HeightCm = 150,
                WeightKg = 40,
            },
            new ChildProfile
            {
                Id = otherStudentId,
                FullName = "Student B",
                Age = 12,
                Grade = "7",
                Gender = Gender.Female,
                SchoolID = otherSchoolId,
                ClassGroupID = otherSchoolClassId,
                ParentUserID = otherParentId,
                HeightCm = 148,
                WeightKg = 39,
            });

        await db.SaveChangesAsync();

        return new SeedIds(schoolUserId, teacherId, otherTeacherId, classId, otherSchoolClassId);
    }

    private sealed record SeedIds(Guid SchoolUserId, Guid TeacherId, Guid OtherTeacherId, Guid ClassId, Guid OtherSchoolClassId);

    private sealed class FakeNotificationService : INotificationService
    {
        public Task CreateAsync(Guid userId, string title, string message, string type, Guid? relatedEntityId = null, string? relatedEntityType = null, string? actionUrl = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyAdminsAsync(string title, string message, string type, Guid? relatedEntityId = null, string? relatedEntityType = null, string? actionUrl = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifySchoolAsync(Guid schoolId, string title, string message, string type, Guid? relatedEntityId = null, string? relatedEntityType = null, string? actionUrl = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyProviderAsync(Guid providerId, string title, string message, string type, Guid? relatedEntityId = null, string? relatedEntityType = null, string? actionUrl = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

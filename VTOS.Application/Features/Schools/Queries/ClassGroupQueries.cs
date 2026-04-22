using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

public class GetSchoolClassesOverviewQuery
{
    public GetSchoolClassesOverviewQuery(Guid userId, string? academicYear = null)
    {
        UserId = userId;
        AcademicYear = academicYear;
    }

    public Guid UserId { get; }
    public string? AcademicYear { get; }
}

public interface IGetSchoolClassesOverviewQueryHandler
{
    Task<Result<SchoolClassesOverviewDto>> HandleAsync(GetSchoolClassesOverviewQuery query, CancellationToken ct = default);
}

public class GetSchoolClassDetailQuery
{
    public GetSchoolClassDetailQuery(Guid userId, Guid classGroupId)
    {
        UserId = userId;
        ClassGroupId = classGroupId;
    }

    public Guid UserId { get; }
    public Guid ClassGroupId { get; }
}

public interface IGetSchoolClassDetailQueryHandler
{
    Task<Result<ClassGroupDetailDto>> HandleAsync(GetSchoolClassDetailQuery query, CancellationToken ct = default);
}

public class GetTeacherClassesOverviewQuery
{
    public GetTeacherClassesOverviewQuery(Guid userId)
    {
        UserId = userId;
    }

    public Guid UserId { get; }
}

public interface IGetTeacherClassesOverviewQueryHandler
{
    Task<Result<TeacherClassesOverviewDto>> HandleAsync(GetTeacherClassesOverviewQuery query, CancellationToken ct = default);
}

public class GetTeacherClassDetailQuery
{
    public GetTeacherClassDetailQuery(Guid userId, Guid classGroupId)
    {
        UserId = userId;
        ClassGroupId = classGroupId;
    }

    public Guid UserId { get; }
    public Guid ClassGroupId { get; }
}

public interface IGetTeacherClassDetailQueryHandler
{
    Task<Result<ClassGroupDetailDto>> HandleAsync(GetTeacherClassDetailQuery query, CancellationToken ct = default);
}

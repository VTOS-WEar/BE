using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// Query to list students belonging to the current school.
/// </summary>
public class GetSchoolStudentsQuery
{
    public Guid UserId { get; }
    public int Page { get; }
    public int PageSize { get; }
    public string? Search { get; }
    public string? Grade { get; }
    public string? MeasurementStatus { get; } // "updated" | "missing"
    public string? ParentLinkStatus { get; }  // "linked" | "unlinked"

    public GetSchoolStudentsQuery(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? grade = null,
        string? measurementStatus = null,
        string? parentLinkStatus = null)
    {
        UserId = userId;
        Page = page;
        PageSize = pageSize;
        Search = search;
        Grade = grade;
        MeasurementStatus = measurementStatus;
        ParentLinkStatus = parentLinkStatus;
    }
}

public interface IGetSchoolStudentsQueryHandler
{
    Task<Result<StudentListResponse>> HandleAsync(GetSchoolStudentsQuery query, CancellationToken ct = default);
}

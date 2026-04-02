using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// DTO returned by the import-status endpoint.
/// Tells the frontend whether the school needs to update student data for the current semester.
/// </summary>
public class ImportStatusDto
{
    public bool NeedsUpdate { get; set; }
    public string CurrentSemester { get; set; } = string.Empty;
    public DateTime? LastImportDate { get; set; }
    public string SuggestedDeadline { get; set; } = string.Empty;
    public int StudentCount { get; set; }
}

/// <summary>
/// Query to check import status for the current school.
/// </summary>
public class GetImportStatusQuery
{
    public Guid UserId { get; }
    public GetImportStatusQuery(Guid userId) => UserId = userId;
}

public interface IGetImportStatusQueryHandler
{
    Task<Result<ImportStatusDto>> HandleAsync(GetImportStatusQuery query, CancellationToken ct = default);
}

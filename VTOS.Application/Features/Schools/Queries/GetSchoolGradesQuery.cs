using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// Query to get distinct grades for the current school.
/// Used to populate a grade combobox on the frontend.
/// </summary>
public class GetSchoolGradesQuery
{
    public Guid UserId { get; }
    public GetSchoolGradesQuery(Guid userId) => UserId = userId;
}

public interface IGetSchoolGradesQueryHandler
{
    Task<Result<IReadOnlyList<string>>> HandleAsync(GetSchoolGradesQuery query, CancellationToken ct = default);
}

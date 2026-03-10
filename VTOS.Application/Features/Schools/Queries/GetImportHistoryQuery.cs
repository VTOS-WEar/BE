using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// Query to get import history (batches) for the current school.
/// </summary>
public class GetImportHistoryQuery
{
    public Guid UserId { get; }
    public int Limit { get; }

    public GetImportHistoryQuery(Guid userId, int limit = 10)
    {
        UserId = userId;
        Limit = limit;
    }
}

public interface IGetImportHistoryQueryHandler
{
    Task<Result<IReadOnlyList<ImportBatchDto>>> HandleAsync(GetImportHistoryQuery query, CancellationToken ct = default);
}

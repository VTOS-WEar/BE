using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// Query to list all outfits belonging to the current school.
/// </summary>
public record GetSchoolOutfitsQuery(
    Guid UserId,
    bool? IsAvailable = null,
    int Page = 1,
    int PageSize = 8,
    string? Search = null,
    Guid? CategoryId = null);

public interface IGetSchoolOutfitsQueryHandler
{
    Task<Result<OutfitListResponse>> HandleAsync(GetSchoolOutfitsQuery query, CancellationToken ct = default);
}

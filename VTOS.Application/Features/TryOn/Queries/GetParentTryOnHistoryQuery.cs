using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.TryOn.Queries;

public record GetParentTryOnHistoryQuery(Guid UserId, int Page = 1, int PageSize = 20);

public record TryOnHistoryDto(
    Guid Id,
    Guid OutfitId,
    string OutfitName,
    string? OutfitImage,
    string? ResultPhotoUrl,
    string? UploadedPhotoUrl,
    DateTime TryOnTimestamp
);

public record GetParentTryOnHistoryResponse(
    IReadOnlyList<TryOnHistoryDto> Items,
    int Total,
    int Page,
    int PageSize
);

public interface IGetParentTryOnHistoryQueryHandler
{
    Task<Result<GetParentTryOnHistoryResponse>> HandleAsync(GetParentTryOnHistoryQuery query, CancellationToken ct = default);
}

public class GetParentTryOnHistoryQueryHandler : IGetParentTryOnHistoryQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetParentTryOnHistoryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<GetParentTryOnHistoryResponse>> HandleAsync(
        GetParentTryOnHistoryQuery query, CancellationToken ct = default)
    {
        var q = _db.TryOnHistories.AsNoTracking()
            .Where(t => t.UserID == query.UserId)
            .OrderByDescending(t => t.TryOnTimestamp);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(t => t.Outfit)
            .Select(t => new TryOnHistoryDto(
                t.Id,
                t.OutfitID,
                t.Outfit != null ? t.Outfit.OutfitName : "Unknown",
                t.Outfit != null ? t.Outfit.MainImageURL : null,
                t.ResultPhotoURL,
                t.UploadedPhotoURL,
                t.TryOnTimestamp
            ))
            .ToListAsync(ct);

        return Result<GetParentTryOnHistoryResponse>.Success(
            new GetParentTryOnHistoryResponse(items, total, query.Page, query.PageSize));
    }
}

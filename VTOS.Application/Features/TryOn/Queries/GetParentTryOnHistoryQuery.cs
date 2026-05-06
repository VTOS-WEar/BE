using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.TryOn.Queries;

public record GetParentTryOnHistoryQuery(Guid UserId, int Page = 1, int PageSize = 20);

public record TryOnHistoryDto(
    Guid Id,
    Guid OutfitId,
    string OutfitName,
    string? OutfitImage,
    string? ResultPhotoUrl,
    string? UploadedPhotoUrl,
    DateTime TryOnTimestamp,
    string Status,
    string? ErrorMessage,
    DateTime? CompletedAt
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
    private readonly ITryOnImageAccessService _tryOnImageAccessService;

    public GetParentTryOnHistoryQueryHandler(
        IApplicationDbContext db,
        ITryOnImageAccessService tryOnImageAccessService)
    {
        _db = db;
        _tryOnImageAccessService = tryOnImageAccessService;
    }

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
            .Select(t => t)
            .ToListAsync(ct);

        var dtos = items.Select(t =>
        {
            var resultUrl = t.Status == TryOnJobStatus.Completed
                ? _tryOnImageAccessService.CreateImageUrl(t, TryOnImageAssetKind.Result) ?? t.ResultPhotoURL
                : null;
            var isRecoverableOrphan = t.UserID != null
                && t.Status == TryOnJobStatus.Completed
                && t.CompletedAt == null
                && string.IsNullOrWhiteSpace(t.ResultPhotoObjectKey)
                && string.IsNullOrWhiteSpace(t.ResultPhotoURL)
                && !string.IsNullOrWhiteSpace(t.UploadedPhotoObjectKey);
            var effectiveStatus = t.Status == TryOnJobStatus.Completed && string.IsNullOrWhiteSpace(resultUrl)
                ? isRecoverableOrphan ? TryOnJobStatus.Queued : TryOnJobStatus.Failed
                : t.Status;
            var errorMessage = effectiveStatus == TryOnJobStatus.Failed && string.IsNullOrWhiteSpace(t.ErrorMessage)
                ? "Không tìm thấy ảnh kết quả thử đồ. Vui lòng thử lại."
                : t.ErrorMessage;

            return new TryOnHistoryDto(
                t.Id,
                t.OutfitID,
                t.Outfit != null ? t.Outfit.OutfitName : "Unknown",
                t.Outfit != null ? t.Outfit.MainImageURL : null,
                effectiveStatus == TryOnJobStatus.Completed ? resultUrl : null,
                _tryOnImageAccessService.CreateImageUrl(t, TryOnImageAssetKind.Uploaded) ?? t.UploadedPhotoURL,
                t.TryOnTimestamp,
                effectiveStatus.ToString(),
                errorMessage,
                t.CompletedAt);
        }).ToList();

        return Result<GetParentTryOnHistoryResponse>.Success(
            new GetParentTryOnHistoryResponse(dtos, total, query.Page, query.PageSize));
    }
}

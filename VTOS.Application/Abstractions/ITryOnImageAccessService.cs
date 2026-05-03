using VTOS.Application.Common;
using VTOS.Domain.Entities;

namespace VTOS.Application.Abstractions;

public enum TryOnImageAssetKind
{
    Uploaded,
    Result
}

public record TryOnImageAccessGrant(
    Guid TryOnId,
    TryOnImageAssetKind AssetKind,
    string ObjectKey,
    string ContentType,
    bool IsGuest);

public interface ITryOnImageAccessService
{
    string? CreateImageUrl(TryOnHistory history, TryOnImageAssetKind assetKind);

    Task<Result<TryOnImageAccessGrant>> ValidateTicketAsync(
        string ticket,
        CancellationToken cancellationToken = default);

    Task<Result<string>> CreateResultImageUrlAsync(
        Guid tryOnId,
        Guid? userId,
        string? guestSessionId,
        CancellationToken cancellationToken = default);
}

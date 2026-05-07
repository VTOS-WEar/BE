using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Queries;

public record GetAdminWalletOwnersQuery(WalletOwnerType OwnerType, string? Search = null, int Page = 1, int PageSize = 20, Guid? OwnerId = null);

public record AdminWalletOwnerDto(
    Guid OwnerId,
    string OwnerType,
    string OwnerName,
    string? Email,
    Guid? WalletId,
    decimal Balance,
    bool IsWalletActive);

public record AdminWalletOwnerListResponse(
    IReadOnlyList<AdminWalletOwnerDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public interface IGetAdminWalletOwnersQueryHandler
{
    Task<Result<AdminWalletOwnerListResponse>> HandleAsync(GetAdminWalletOwnersQuery query, CancellationToken ct = default);
}

public class GetAdminWalletOwnersQueryHandler : IGetAdminWalletOwnersQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetAdminWalletOwnersQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<AdminWalletOwnerListResponse>> HandleAsync(GetAdminWalletOwnersQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var search = query.Search?.Trim().ToLowerInvariant();

        if (query.OwnerType == WalletOwnerType.Parent)
            return Result<AdminWalletOwnerListResponse>.Success(await GetParentsAsync(search, page, pageSize, query.OwnerId, ct));

        if (query.OwnerType == WalletOwnerType.Provider)
            return Result<AdminWalletOwnerListResponse>.Success(await GetProvidersAsync(search, page, pageSize, query.OwnerId, ct));

        return Result<AdminWalletOwnerListResponse>.Failure("Only Parent and Provider wallets can be credited manually.", "INVALID_OWNER_TYPE");
    }

    private async Task<AdminWalletOwnerListResponse> GetParentsAsync(string? search, int page, int pageSize, Guid? ownerId, CancellationToken ct)
    {
        var query =
            from user in _db.Users.AsNoTracking()
            join wallet in _db.Wallets.AsNoTracking().Where(w => w.OwnerType == WalletOwnerType.Parent)
                on user.Id equals wallet.OwnerID into wallets
            from wallet in wallets.DefaultIfEmpty()
            where user.Role.RoleName == "Parent" && user.IsActive && !user.IsDeleted
            select new { user, wallet };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.user.FullName.ToLower().Contains(search) ||
                x.user.Email.ToLower().Contains(search));
        }
        if (ownerId.HasValue)
            query = query.Where(x => x.user.Id == ownerId.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.user.FullName)
            .ThenBy(x => x.user.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminWalletOwnerDto(
                x.user.Id,
                WalletOwnerType.Parent.ToString(),
                string.IsNullOrWhiteSpace(x.user.FullName) ? x.user.Email : x.user.FullName,
                x.user.Email,
                x.wallet != null ? (Guid?)x.wallet.Id : null,
                x.wallet != null ? x.wallet.Balance : 0,
                x.wallet != null && x.wallet.IsActive))
            .ToListAsync(ct);

        return new AdminWalletOwnerListResponse(items, totalCount, page, pageSize);
    }

    private async Task<AdminWalletOwnerListResponse> GetProvidersAsync(string? search, int page, int pageSize, Guid? ownerId, CancellationToken ct)
    {
        var query =
            from provider in _db.Providers.AsNoTracking()
            join wallet in _db.Wallets.AsNoTracking().Where(w => w.OwnerType == WalletOwnerType.Provider)
                on provider.Id equals wallet.OwnerID into wallets
            from wallet in wallets.DefaultIfEmpty()
            where !provider.IsDeleted && provider.Status == ProviderStatus.Active
            select new { provider, wallet };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.provider.ProviderName.ToLower().Contains(search) ||
                (x.provider.Email != null && x.provider.Email.ToLower().Contains(search)) ||
                (x.provider.ContactPersonName != null && x.provider.ContactPersonName.ToLower().Contains(search)));
        }
        if (ownerId.HasValue)
            query = query.Where(x => x.provider.Id == ownerId.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.provider.ProviderName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminWalletOwnerDto(
                x.provider.Id,
                WalletOwnerType.Provider.ToString(),
                x.provider.ProviderName,
                x.provider.Email,
                x.wallet != null ? (Guid?)x.wallet.Id : null,
                x.wallet != null ? x.wallet.Balance : 0,
                x.wallet != null && x.wallet.IsActive))
            .ToListAsync(ct);

        return new AdminWalletOwnerListResponse(items, totalCount, page, pageSize);
    }
}

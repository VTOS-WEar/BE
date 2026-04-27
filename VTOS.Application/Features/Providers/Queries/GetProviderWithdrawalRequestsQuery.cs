using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Queries;

public record GetProviderWithdrawalRequestsQuery(Guid UserId, int Page = 1, int PageSize = 10, string? Status = null);

public record ProviderWithdrawalRequestDto(
    Guid WithdrawalRequestId,
    Guid WalletId,
    decimal Amount,
    string Status,
    string? BankCode,
    string? BankName,
    string? BankAccountNumber,
    string? BankAccountName,
    DateTime RequestedAt,
    DateTime? ApprovedAt,
    DateTime? PaidAt,
    string? AdminNote
);

public record ProviderWithdrawalRequestsResponse(
    List<ProviderWithdrawalRequestDto> Items,
    int Total,
    int Page,
    int PageSize
);

public interface IGetProviderWithdrawalRequestsQueryHandler
{
    Task<Result<ProviderWithdrawalRequestsResponse>> HandleAsync(GetProviderWithdrawalRequestsQuery query, CancellationToken ct = default);
}

public class GetProviderWithdrawalRequestsQueryHandler : IGetProviderWithdrawalRequestsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetProviderWithdrawalRequestsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ProviderWithdrawalRequestsResponse>> HandleAsync(GetProviderWithdrawalRequestsQuery query, CancellationToken ct = default)
    {
        var providerMgr = await _db.ProviderManagers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserID == query.UserId, ct);

        if (providerMgr == null)
            return Result<ProviderWithdrawalRequestsResponse>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var wallet = await _db.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.OwnerID == providerMgr.ProviderID && w.OwnerType == WalletOwnerType.Provider && w.IsActive, ct);

        if (wallet == null)
            return Result<ProviderWithdrawalRequestsResponse>.Success(
                new ProviderWithdrawalRequestsResponse(new(), 0, Math.Max(1, query.Page), Math.Clamp(query.PageSize, 1, 50)));

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var requestsQuery = _db.WalletWithdrawalRequests
            .AsNoTracking()
            .Where(wr => wr.WalletID == wallet.Id);

        if (!string.IsNullOrWhiteSpace(query.Status))
            requestsQuery = requestsQuery.Where(wr => wr.Status == query.Status);

        var total = await requestsQuery.CountAsync(ct);
        var items = await requestsQuery
            .OrderByDescending(wr => wr.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(wr => new ProviderWithdrawalRequestDto(
                wr.Id,
                wr.WalletID,
                wr.Amount,
                wr.Status,
                wallet.BankCode,
                wallet.BankName,
                wallet.BankAccountNumber,
                wallet.BankAccountName,
                wr.RequestedAt,
                wr.ApprovedAt,
                wr.PaidAt,
                wr.AdminNote
            ))
            .ToListAsync(ct);

        return Result<ProviderWithdrawalRequestsResponse>.Success(
            new ProviderWithdrawalRequestsResponse(items, total, page, pageSize));
    }
}

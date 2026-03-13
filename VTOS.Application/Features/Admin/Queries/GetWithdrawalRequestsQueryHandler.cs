using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Admin.Queries;

public class GetWithdrawalRequestsQueryHandler : IGetWithdrawalRequestsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetWithdrawalRequestsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<WithdrawalRequestListResponse> HandleAsync(GetWithdrawalRequestsQuery query, CancellationToken ct = default)
    {
        var withdrawalsQuery = _db.Set<WalletWithdrawalRequest>()
            .AsNoTracking()
            .Include(w => w.Wallet)
                .ThenInclude(wallet => wallet.School)
            .AsQueryable();

        // Apply status filter
        if (!string.IsNullOrEmpty(query.Status))
        {
            withdrawalsQuery = withdrawalsQuery.Where(w => w.Status == query.Status);
        }

        var totalCount = await withdrawalsQuery.CountAsync(ct);

        var items = await withdrawalsQuery
            .OrderByDescending(w => w.RequestedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(w => new WithdrawalRequestDto
            {
                WithdrawalRequestId = w.Id,
                WalletId = w.WalletID,
                SchoolId = w.Wallet.SchoolID,
                SchoolName = w.Wallet.School.SchoolName,
                Amount = w.Amount,
                Status = w.Status,
                BankCode = w.Wallet.BankCode,
                BankName = w.Wallet.BankName,
                BankAccountNumber = w.Wallet.BankAccountNumber,
                BankAccountName = w.Wallet.BankAccountName,
                RequestedAt = w.RequestedAt,
                ApprovedAt = w.ApprovedAt,
                PaidAt = w.PaidAt,
                AdminNote = w.AdminNote
            })
            .ToListAsync(ct);

        return new WithdrawalRequestListResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}

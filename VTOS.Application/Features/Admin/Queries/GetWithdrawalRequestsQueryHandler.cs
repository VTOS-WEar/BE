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
        // Manual join since Wallet nav properties (School/Provider) are ignored for polymorphic ownership
        var baseQuery = from wr in _db.Set<WalletWithdrawalRequest>().AsNoTracking()
                        join w in _db.Wallets.AsNoTracking() on wr.WalletID equals w.Id
                        join s in _db.Schools.AsNoTracking() on w.OwnerID equals s.Id into schools
                        from s in schools.DefaultIfEmpty()
                        select new { wr, w, SchoolName = s != null ? s.SchoolName : "Unknown" };

        // Apply status filter
        if (!string.IsNullOrEmpty(query.Status))
        {
            baseQuery = baseQuery.Where(x => x.wr.Status == query.Status);
        }

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(x => x.wr.RequestedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new WithdrawalRequestDto
            {
                WithdrawalRequestId = x.wr.Id,
                WalletId = x.wr.WalletID,
                SchoolId = x.w.OwnerID,
                SchoolName = x.SchoolName,
                Amount = x.wr.Amount,
                Status = x.wr.Status,
                BankCode = x.w.BankCode,
                BankName = x.w.BankName,
                BankAccountNumber = x.w.BankAccountNumber,
                BankAccountName = x.w.BankAccountName,
                RequestedAt = x.wr.RequestedAt,
                ApprovedAt = x.wr.ApprovedAt,
                PaidAt = x.wr.PaidAt,
                AdminNote = x.wr.AdminNote
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

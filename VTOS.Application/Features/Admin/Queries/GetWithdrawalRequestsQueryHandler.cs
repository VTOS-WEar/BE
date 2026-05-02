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
        // Manual join since Wallet nav properties are ignored for polymorphic ownership.
        var baseQuery = from wr in _db.Set<WalletWithdrawalRequest>().AsNoTracking()
                        join w in _db.Wallets.AsNoTracking() on wr.WalletID equals w.Id
                        join s in _db.Schools.AsNoTracking() on w.OwnerID equals s.Id into schools
                        from s in schools.DefaultIfEmpty()
                        join p in _db.Providers.AsNoTracking() on w.OwnerID equals p.Id into providers
                        from p in providers.DefaultIfEmpty()
                        join u in _db.Users.AsNoTracking() on w.OwnerID equals u.Id into parents
                        from u in parents.DefaultIfEmpty()
                        select new
                        {
                            wr,
                            w,
                            OwnerName = s != null
                                ? s.SchoolName
                                : p != null
                                    ? p.ProviderName
                                    : u != null
                                        ? u.FullName
                                        : "Unknown"
                        };

        // Apply status filter
        if (!string.IsNullOrEmpty(query.Status))
        {
            baseQuery = baseQuery.Where(x => x.wr.Status == query.Status);
        }

        // Apply search filter on owner name or bank name
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(x =>
                x.OwnerName.ToLower().Contains(search) ||
                (x.w.BankName != null && x.w.BankName.ToLower().Contains(search)) ||
                (x.w.BankAccountNumber != null && x.w.BankAccountNumber.ToLower().Contains(search)));
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
                SchoolName = x.OwnerName,
                OwnerId = x.w.OwnerID,
                OwnerType = x.w.OwnerType.ToString(),
                OwnerName = x.OwnerName,
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

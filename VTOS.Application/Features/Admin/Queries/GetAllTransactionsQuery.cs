using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Queries;

// ── DTOs ──

public record AdminTransactionDto
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? OrderId { get; init; }
    public string? OrderCode { get; init; }
    public string? WalletOwner { get; init; }
    public string? Description { get; init; }
    public string? PaymentLinkId { get; init; }
}

public record AdminTransactionListResult
{
    public List<AdminTransactionDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public decimal TotalAmountAll { get; init; }
    public int TodayCount { get; init; }
}

// ── Interface ──

public interface IGetAllTransactionsQueryHandler
{
    Task<Result<AdminTransactionListResult>> HandleAsync(
        int page = 1, int pageSize = 20,
        TransactionType? type = null, PaymentStatus? status = null,
        DateTime? from = null, DateTime? to = null,
        CancellationToken ct = default);
}

// ── Handler ──

public class GetAllTransactionsQueryHandler : IGetAllTransactionsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetAllTransactionsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<AdminTransactionListResult>> HandleAsync(
        int page = 1, int pageSize = 20,
        TransactionType? type = null, PaymentStatus? status = null,
        DateTime? from = null, DateTime? to = null,
        CancellationToken ct = default)
    {
        var query = _context.PaymentTransactions
            .Include(t => t.Order)
            .Include(t => t.Wallet)
                .ThenInclude(w => w!.School)
            .Include(t => t.Wallet)
                .ThenInclude(w => w!.Provider)
            .AsQueryable();

        if (type.HasValue)
            query = query.Where(t => t.TransactionType == type.Value);
        if (status.HasValue)
            query = query.Where(t => t.TransactionStatus == status.Value);
        if (from.HasValue)
            query = query.Where(t => t.TransactionTimestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.TransactionTimestamp <= to.Value);

        var totalCount = await query.CountAsync(ct);
        var totalAmount = await query.SumAsync(t => t.Amount, ct);

        var todayStart = DateTime.UtcNow.Date;
        var todayCount = await query.CountAsync(t => t.TransactionTimestamp >= todayStart, ct);

        var items = await query
            .OrderByDescending(t => t.TransactionTimestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new AdminTransactionDto
            {
                Id = t.Id,
                CreatedAt = t.TransactionTimestamp,
                TransactionType = t.TransactionType.ToString(),
                Amount = t.Amount,
                Status = t.TransactionStatus.ToString(),
                OrderId = t.OrderID,
                OrderCode = t.OrderID.HasValue ? t.OrderID.Value.ToString().Substring(0, 8).ToUpper() : null,
                WalletOwner = t.Wallet != null
                    ? (t.Wallet.School != null ? t.Wallet.School.SchoolName
                       : t.Wallet.Provider != null ? t.Wallet.Provider.ProviderName : null)
                    : null,
                Description = t.Description,
                PaymentLinkId = t.PaymentLinkId
            })
            .ToListAsync(ct);

        return Result<AdminTransactionListResult>.Success(new AdminTransactionListResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalAmountAll = totalAmount,
            TodayCount = todayCount
        });
    }
}

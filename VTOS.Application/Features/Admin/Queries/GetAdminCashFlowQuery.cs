using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Queries;

// ── DTOs ──

public record AdminCashFlowDto
{
    public decimal TotalParentPayments { get; init; }      // Sum of completed OrderPayment
    public decimal TotalProviderPayments { get; init; }    // Sum of completed ProviderPayment
    public decimal TotalRefunds { get; init; }             // Sum of completed Refund
    public decimal PendingPayments { get; init; }          // Sum of pending OrderPayment
    public int TotalTransactionCount { get; init; }
    public int PendingComplaintCount { get; init; }
    public int ActiveCampaignCount { get; init; }
    public int PendingAccountRequestCount { get; init; }
    public List<DailyRevenueDto> RevenueChart { get; init; } = new();
}

public record DailyRevenueDto
{
    public string Date { get; init; } = string.Empty;
    public decimal Income { get; init; }
    public decimal Expense { get; init; }
}

// ── Interface ──

public interface IGetAdminCashFlowQueryHandler
{
    Task<Result<AdminCashFlowDto>> HandleAsync(int days = 30, CancellationToken ct = default);
}

// ── Handler ──

public class GetAdminCashFlowQueryHandler : IGetAdminCashFlowQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetAdminCashFlowQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<AdminCashFlowDto>> HandleAsync(int days = 30, CancellationToken ct = default)
    {
        var completedTxns = await _context.PaymentTransactions
            .Where(t => t.TransactionStatus == PaymentStatus.Completed)
            .ToListAsync(ct);

        var totalParent = completedTxns.Where(t => t.TransactionType == TransactionType.OrderPayment).Sum(t => t.Amount);
        var totalProvider = completedTxns.Where(t => t.TransactionType == TransactionType.ProviderPayment).Sum(t => t.Amount);
        var totalRefunds = completedTxns.Where(t => t.TransactionType == TransactionType.Refund).Sum(t => t.Amount);

        var pendingPayments = await _context.PaymentTransactions
            .Where(t => t.TransactionStatus == PaymentStatus.Pending && t.TransactionType == TransactionType.OrderPayment)
            .SumAsync(t => t.Amount, ct);

        var totalCount = await _context.PaymentTransactions.CountAsync(ct);

        var pendingComplaints = await _context.Complaints
            .CountAsync(c => c.Status == ComplaintStatus.Open || c.Status == ComplaintStatus.InProgress, ct);

        var activeCampaigns = await _context.Campaigns
            .CountAsync(c => c.Status == CampaignStatus.Active, ct);

        var pendingRequests = await _context.AccountRequests
            .CountAsync(a => a.Status == Domain.Enums.AccountRequestStatus.Pending, ct);

        // Revenue chart — last N days
        var startDate = DateTime.UtcNow.Date.AddDays(-days);
        var recentTxns = completedTxns
            .Where(t => t.TransactionTimestamp >= startDate)
            .GroupBy(t => t.TransactionTimestamp.Date)
            .Select(g => new DailyRevenueDto
            {
                Date = g.Key.ToString("dd/MM"),
                Income = g.Where(t => t.TransactionType == TransactionType.OrderPayment).Sum(t => t.Amount),
                Expense = g.Where(t => t.TransactionType == TransactionType.ProviderPayment || t.TransactionType == TransactionType.Refund).Sum(t => t.Amount)
            })
            .OrderBy(d => d.Date)
            .ToList();

        return Result<AdminCashFlowDto>.Success(new AdminCashFlowDto
        {
            TotalParentPayments = totalParent,
            TotalProviderPayments = totalProvider,
            TotalRefunds = totalRefunds,
            PendingPayments = pendingPayments,
            TotalTransactionCount = totalCount,
            PendingComplaintCount = pendingComplaints,
            ActiveCampaignCount = activeCampaigns,
            PendingAccountRequestCount = pendingRequests,
            RevenueChart = recentTxns
        });
    }
}

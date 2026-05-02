using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Queries;

public record AdminCashFlowDto
{
    public decimal TotalOrderFees { get; init; }
    public decimal TotalWithdrawalFees { get; init; }
    public decimal TotalPlatformFees { get; init; }
    public int TotalTransactionCount { get; init; }
    public int PendingComplaintCount { get; init; }
    public int ActiveCampaignCount { get; init; }
    public int PendingAccountRequestCount { get; init; }
    public List<DailyRevenueDto> RevenueChart { get; init; } = new();
}

public record DailyRevenueDto
{
    public string Date { get; init; } = string.Empty;
    public decimal OrderFees { get; init; }
    public decimal WithdrawalFees { get; init; }
    public decimal TotalFees { get; init; }
}

public interface IGetAdminCashFlowQueryHandler
{
    Task<Result<AdminCashFlowDto>> HandleAsync(int days = 30, CancellationToken ct = default);
}

public class GetAdminCashFlowQueryHandler : IGetAdminCashFlowQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetAdminCashFlowQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<AdminCashFlowDto>> HandleAsync(int days = 30, CancellationToken ct = default)
    {
        var feeTxns = await _context.PaymentTransactions
            .Where(t =>
                t.TransactionStatus == PaymentStatus.Completed &&
                (t.TransactionType == TransactionType.PlatformOrderFee ||
                 t.TransactionType == TransactionType.ProviderWithdrawalFee))
            .ToListAsync(ct);

        var totalOrderFees = feeTxns
            .Where(t => t.TransactionType == TransactionType.PlatformOrderFee)
            .Sum(t => t.Amount);

        var totalWithdrawalFees = feeTxns
            .Where(t => t.TransactionType == TransactionType.ProviderWithdrawalFee)
            .Sum(t => t.Amount);

        var totalCount = await _context.PaymentTransactions.CountAsync(ct);

        var pendingComplaints = await _context.SupportTickets
            .CountAsync(c => c.Status == SupportTicketStatus.Open || c.Status == SupportTicketStatus.InProgress, ct);

        var activeCampaigns = await _context.SemesterPublications
            .CountAsync(sp => sp.Status == SemesterPublicationStatus.Active, ct);

        var pendingRequests = await _context.AccountRequests
            .CountAsync(a => a.Status == Domain.Enums.AccountRequestStatus.Pending, ct);

        var startDate = DateTime.UtcNow.Date.AddDays(-days);
        var revenueChart = feeTxns
            .Where(t => t.TransactionTimestamp >= startDate)
            .GroupBy(t => t.TransactionTimestamp.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var orderFees = g.Where(t => t.TransactionType == TransactionType.PlatformOrderFee).Sum(t => t.Amount);
                var withdrawalFees = g.Where(t => t.TransactionType == TransactionType.ProviderWithdrawalFee).Sum(t => t.Amount);
                return new DailyRevenueDto
                {
                    Date = g.Key.ToString("dd/MM"),
                    OrderFees = orderFees,
                    WithdrawalFees = withdrawalFees,
                    TotalFees = orderFees + withdrawalFees
                };
            })
            .ToList();

        return Result<AdminCashFlowDto>.Success(new AdminCashFlowDto
        {
            TotalOrderFees = totalOrderFees,
            TotalWithdrawalFees = totalWithdrawalFees,
            TotalPlatformFees = totalOrderFees + totalWithdrawalFees,
            TotalTransactionCount = totalCount,
            PendingComplaintCount = pendingComplaints,
            ActiveCampaignCount = activeCampaigns,
            PendingAccountRequestCount = pendingRequests,
            RevenueChart = revenueChart
        });
    }
}

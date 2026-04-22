using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Payments.Commands;
using VTOS.Domain.Enums;

namespace VTOS.Infrastructure.BackgroundJobs;

public class AutoPayoutJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoPayoutJob> _logger;
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan DisputeWindow = TimeSpan.FromDays(7);

    public AutoPayoutJob(IServiceProvider serviceProvider, ILogger<AutoPayoutJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(RunInterval, stoppingToken);
                await RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoPayoutJob cycle");
            }
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var payoutService = scope.ServiceProvider.GetRequiredService<IProviderPayoutService>();
        var cutoff = DateTime.UtcNow.Subtract(DisputeWindow);

        var eligibleOrderIds = await db.Orders
            .AsNoTracking()
            .Where(o => o.OrderStatus == OrderStatus.Delivered &&
                        !o.IsProviderPaid &&
                        (o.UpdatedAt ?? o.OrderDate) <= cutoff)
            .Join(
                db.PaymentTransactions.AsNoTracking()
                    .Where(pt =>
                        pt.OrderID.HasValue &&
                        pt.TransactionType == TransactionType.OrderPayment &&
                        pt.TransactionStatus == PaymentStatus.Completed &&
                        pt.EscrowStatus == EscrowStatus.Held),
                o => o.Id,
                pt => pt.OrderID!.Value,
                (o, pt) => o.Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (eligibleOrderIds.Count == 0)
            return;

        var releasedCount = 0;
        foreach (var orderId in eligibleOrderIds)
        {
            var result = await payoutService.ReleaseOrderPayoutAsync(
                orderId,
                DateTime.UtcNow,
                "Auto payout after 7-day dispute window.",
                requireDisputeWindow: true,
                cancellationToken);

            if (result.IsSuccess)
            {
                releasedCount++;
            }
            else
            {
                _logger.LogWarning(
                    "Auto payout skipped for order {OrderId}: {Code} {Error}",
                    orderId,
                    result.ErrorCode,
                    result.Error);
            }
        }

        _logger.LogInformation("AutoPayoutJob released {Count} payouts.", releasedCount);
    }
}

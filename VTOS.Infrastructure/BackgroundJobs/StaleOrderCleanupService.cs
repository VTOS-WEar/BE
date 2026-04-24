using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Infrastructure.BackgroundJobs;

public class StaleOrderCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StaleOrderCleanupService> _logger;
    private static readonly TimeSpan RunInterval = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromHours(2);

    public StaleOrderCleanupService(IServiceProvider serviceProvider, ILogger<StaleOrderCleanupService> logger)
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
                await CleanupStaleOrdersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StaleOrderCleanupService cycle");
            }
        }
    }

    private async Task CleanupStaleOrdersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var payOSService = scope.ServiceProvider.GetRequiredService<IPayOSService>();

        var cutoff = DateTime.UtcNow.Subtract(StaleThreshold);
        var staleOrders = await context.Orders
            .Include(o => o.ChildProfile)
            .Include(o => o.PaymentTransactions)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant)
            .Where(o => o.OrderStatus == OrderStatus.Pending && o.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var order in staleOrders)
        {
            try
            {
                await ProcessStaleOrderAsync(order, payOSService, context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing stale order {OrderId}", order.Id);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessStaleOrderAsync(
        Order order,
        IPayOSService payOSService,
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var pendingTransaction = order.PaymentTransactions
            .FirstOrDefault(t => t.TransactionStatus == PaymentStatus.Pending || t.TransactionStatus == PaymentStatus.Processing);

        if (pendingTransaction == null || string.IsNullOrEmpty(pendingTransaction.PaymentLinkId))
        {
            CancelOrder(order);
            return;
        }

        try
        {
            var paymentInfo = await payOSService.GetPaymentLinkInfoAsync(pendingTransaction.PaymentLinkId, cancellationToken);
            var payosStatus = paymentInfo?.Status?.ToUpperInvariant();

            switch (payosStatus)
            {
                case "PAID":
                    pendingTransaction.TransactionStatus = PaymentStatus.Completed;
                    pendingTransaction.TransactionTimestamp = DateTime.UtcNow;
                    pendingTransaction.TransactionType = TransactionType.OrderPayment;
                    pendingTransaction.EscrowStatus = EscrowStatus.Held;
                    pendingTransaction.TransactionLog = "Payment confirmed via stale order cleanup";
                    pendingTransaction.UpdatedAt = DateTime.UtcNow;
                    order.OrderStatus = OrderStatus.Paid;
                    order.UpdatedAt = DateTime.UtcNow;

                    pendingTransaction.WalletID = null;

                    foreach (var item in order.OrderItems)
                    {
                        if (item.ProductVariant != null)
                            item.ProductVariant.StockQuantity -= item.Quantity;
                    }
                    break;

                case "CANCELLED":
                case "EXPIRED":
                    CancelOrder(order);
                    break;

                default:
                    try
                    {
                        await payOSService.CancelPaymentLinkAsync(pendingTransaction.PaymentLinkId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cancel PayOS link for stale order {OrderId}", order.Id);
                    }
                    CancelOrder(order);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check PayOS status for stale order {OrderId}. Cancelling.", order.Id);
            CancelOrder(order);
        }
    }

    private static void CancelOrder(Order order)
    {
        order.OrderStatus = OrderStatus.Cancelled;
        order.CancelReason = "Auto-cancelled after payment timeout";
        order.UpdatedAt = DateTime.UtcNow;

        foreach (var tx in order.PaymentTransactions.Where(t => t.TransactionStatus == PaymentStatus.Pending || t.TransactionStatus == PaymentStatus.Processing))
        {
            tx.TransactionStatus = PaymentStatus.Cancelled;
            tx.TransactionLog = "Auto-cancelled by stale order cleanup";
            tx.UpdatedAt = DateTime.UtcNow;
        }
    }
}

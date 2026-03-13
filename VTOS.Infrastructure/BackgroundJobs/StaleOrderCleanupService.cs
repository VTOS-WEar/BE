using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Domain.Enums;

namespace VTOS.Infrastructure.BackgroundJobs;

/// <summary>
/// Background service that periodically checks for stale PENDING orders
/// and reconciles their status with PayOS.
/// 
/// Handles cases where:
/// - User closes browser mid-payment (no webhook, no cancel page)
/// - PayOS link expires/times out
/// - PayOS webhook was missed (server was down)
/// </summary>
public class StaleOrderCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StaleOrderCleanupService> _logger;

    /// <summary>How often the cleanup runs</summary>
    private static readonly TimeSpan RunInterval = TimeSpan.FromMinutes(30);

    /// <summary>Orders older than this are considered stale</summary>
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromHours(2);

    public StaleOrderCleanupService(
        IServiceProvider serviceProvider,
        ILogger<StaleOrderCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StaleOrderCleanupService started. Interval: {Interval}min, Threshold: {Threshold}h",
            RunInterval.TotalMinutes, StaleThreshold.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(RunInterval, stoppingToken);
                await CleanupStaleOrdersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected on shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StaleOrderCleanupService cycle");
            }
        }

        _logger.LogInformation("StaleOrderCleanupService stopped.");
    }

    private async Task CleanupStaleOrdersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var payOSService = scope.ServiceProvider.GetRequiredService<IPayOSService>();

        var cutoff = DateTime.UtcNow.Subtract(StaleThreshold);

        // Find PENDING orders older than threshold
        var staleOrders = await context.Orders
            .Include(o => o.PaymentTransactions)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant)
            .Where(o => o.OrderStatus == OrderStatus.Pending && o.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (staleOrders.Count == 0)
        {
            _logger.LogDebug("No stale PENDING orders found (threshold: {Cutoff})", cutoff);
            return;
        }

        _logger.LogInformation("Found {Count} stale PENDING orders to process", staleOrders.Count);

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
        _logger.LogInformation("Stale order cleanup completed. Processed {Count} orders.", staleOrders.Count);
    }

    private async Task ProcessStaleOrderAsync(
        Domain.Entities.Order order,
        IPayOSService payOSService,
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var pendingTransaction = order.PaymentTransactions
            .FirstOrDefault(t => t.TransactionStatus == PaymentStatus.Pending
                              || t.TransactionStatus == PaymentStatus.Processing);

        if (pendingTransaction == null || string.IsNullOrEmpty(pendingTransaction.PaymentLinkId))
        {
            // No payment link — just cancel the order
            _logger.LogWarning("Stale order {OrderId} has no payment link. Cancelling.", order.Id);
            CancelOrder(order);
            return;
        }

        // Check actual status from PayOS
        try
        {
            var paymentInfo = await payOSService.GetPaymentLinkInfoAsync(
                pendingTransaction.PaymentLinkId, cancellationToken);

            var payosStatus = paymentInfo?.Status?.ToUpperInvariant();
            _logger.LogInformation("Stale order {OrderId} PayOS status: {Status}", order.Id, payosStatus);

            switch (payosStatus)
            {
                case "PAID":
                    // Webhook was missed — mark as paid
                    _logger.LogWarning("Order {OrderId} was actually PAID but webhook missed! Updating...", order.Id);
                    pendingTransaction.TransactionStatus = PaymentStatus.Completed;
                    pendingTransaction.TransactionTimestamp = DateTime.UtcNow;
                    pendingTransaction.TransactionLog = "Payment confirmed via stale order cleanup (webhook missed)";
                    pendingTransaction.UpdatedAt = DateTime.UtcNow;
                    order.OrderStatus = OrderStatus.Paid;
                    order.UpdatedAt = DateTime.UtcNow;

                    // Update wallet if applicable
                    if (pendingTransaction.Wallet != null)
                    {
                        pendingTransaction.Wallet.Balance += pendingTransaction.Amount;
                        pendingTransaction.Wallet.UpdatedAt = DateTime.UtcNow;
                    }

                    // Update stock
                    if (order.OrderItems != null)
                    {
                        foreach (var item in order.OrderItems)
                        {
                            if (item.ProductVariant != null)
                            {
                                item.ProductVariant.StockQuantity -= item.Quantity;
                            }
                        }
                    }
                    break;

                case "CANCELLED":
                case "EXPIRED":
                    _logger.LogInformation("Stale order {OrderId} is {Status} on PayOS. Cancelling locally.", order.Id, payosStatus);
                    CancelOrder(order);
                    break;

                default:
                    // Still PENDING on PayOS — cancel it
                    _logger.LogInformation("Stale order {OrderId} still PENDING on PayOS. Cancelling...", order.Id);
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

    private static void CancelOrder(Domain.Entities.Order order)
    {
        order.OrderStatus = OrderStatus.Cancelled;
        order.CancelReason = "Tự động huỷ — hết thời gian thanh toán";
        order.UpdatedAt = DateTime.UtcNow;

        foreach (var tx in order.PaymentTransactions
            .Where(t => t.TransactionStatus == PaymentStatus.Pending
                     || t.TransactionStatus == PaymentStatus.Processing))
        {
            tx.TransactionStatus = PaymentStatus.Cancelled;
            tx.TransactionLog = "Auto-cancelled by stale order cleanup";
            tx.UpdatedAt = DateTime.UtcNow;
        }
    }
}

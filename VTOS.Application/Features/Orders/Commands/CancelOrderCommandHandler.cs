using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Common.Models;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Orders.Commands;

/// <summary>
/// Handler for cancel order command with two cases:
/// Case 1 (Pending - not yet paid): Cancel PayOS payment link + update status to Cancelled
/// Case 2 (Confirmed - already paid): Update status to Cancelled + create Refund request
/// </summary>
public class CancelOrderCommandHandler : ICancelOrderCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPayOSService _payOSService;
    private readonly ILogger<CancelOrderCommandHandler> _logger;

    public CancelOrderCommandHandler(
        IApplicationDbContext context,
        IPayOSService payOSService,
        ILogger<CancelOrderCommandHandler> logger)
    {
        _context = context;
        _payOSService = payOSService;
        _logger = logger;
    }

    public async Task<Result<List<RefundResponse>>> HandleAsync(CancelOrderCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Load order with related data
            var order = await _context.Orders
                .Include(o => o.ChildProfile)
                .Include(o => o.PaymentTransactions)
                .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

            if (order == null)
                return Result<List<RefundResponse>>.Failure("Order not found", "ORDER_NOT_FOUND");

            // Step 2: Validate ownership
            if (order.ChildProfile.ParentUserID != command.ParentId)
            {
                _logger.LogWarning("Unauthorized cancel attempt: Parent {ParentId} on Order {OrderId}",
                    command.ParentId, command.OrderId);
                return Result<List<RefundResponse>>.Failure("You are not authorized to cancel this order", "UNAUTHORIZED_ORDER_ACCESS");
            }

            // Step 3: Route to the correct cancellation flow based on current status
            List<RefundResponse> refunds;
            switch (order.OrderStatus)
            {
                case OrderStatus.Pending:
                    var pendingResult = await HandlePendingCancellationAsync(order, cancellationToken);
                    if (!pendingResult.IsSuccess)
                        return Result<List<RefundResponse>>.Failure(pendingResult.Error!, pendingResult.ErrorCode);
                    refunds = new List<RefundResponse>();
                    break;
                case OrderStatus.Paid:
                    var paidResult = HandlePaidCancellation(order, command.Reason);
                    if (!paidResult.IsSuccess)
                        return Result<List<RefundResponse>>.Failure(paidResult.Error!, paidResult.ErrorCode);
                    refunds = paidResult.Value!;
                    break;
                default:
                    return Result<List<RefundResponse>>.Failure($"Order cannot be cancelled. Current status: {order.OrderStatus}", "ORDER_NOT_CANCELLABLE");
            }

            // Step 4: Set cancel reason
            order.CancelReason = command.Reason;

            // Step 5: Save all changes
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} cancelled successfully by Parent {ParentId}",
                command.OrderId, command.ParentId);

            return Result<List<RefundResponse>>.Success(refunds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", command.OrderId);
            return Result<List<RefundResponse>>.Failure($"Failed to cancel order: {ex.Message}", "CANCEL_ORDER_ERROR");
        }
    }

    /// <summary>
    /// Case 1: Order is Pending (not yet paid)
    /// - Send cancel payment link request to PayOS
    /// - Update Order status → Cancelled
    /// - Update PaymentTransaction status → Cancelled
    /// </summary>
    private async Task<Result> HandlePendingCancellationAsync(Order order, CancellationToken cancellationToken)
    {
        // Cancel PayOS payment link
        var pendingTransaction = order.PaymentTransactions
            .FirstOrDefault(t => t.TransactionStatus == PaymentStatus.Pending
                              || t.TransactionStatus == PaymentStatus.Processing);

        if (pendingTransaction != null && !string.IsNullOrEmpty(pendingTransaction.PaymentLinkId))
        {
            try
            {
                var cancelResponse = await _payOSService.CancelPaymentLinkAsync(
                    pendingTransaction.PaymentLinkId, cancellationToken);

                _logger.LogInformation("PayOS payment link cancelled: {PaymentLinkId}, Status={Status}",
                    pendingTransaction.PaymentLinkId, cancelResponse?.Status);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to cancel PayOS payment link {PaymentLinkId}. Proceeding with local cancellation.",
                    pendingTransaction.PaymentLinkId);
            }
        }

        // Update statuses
        SetOrderCancelled(order);
        CancelPendingTransactions(order);

        return Result.Success();
    }

    /// <summary>
    /// Case 2: Order is Paid (already paid)
    /// - Do NOT send cancel to PayOS (payment already completed)
    /// - Update Order status → Cancelled
    /// - Update PaymentTransaction status → Cancelled  
    /// - Create Refund request for completed transactions
    /// </summary>
    private Result<List<RefundResponse>> HandlePaidCancellation(Order order, string? reason)
    {
        SetOrderCancelled(order);

        // Create Refund for each completed transaction
        var completedTransactions = order.PaymentTransactions
            .Where(t => t.TransactionStatus == PaymentStatus.Completed)
            .ToList();

        var refundResponses = new List<RefundResponse>();

        foreach (var transaction in completedTransactions)
        {
            var refund = new Refund
            {
                Id = Guid.NewGuid(),
                PaymentID = transaction.Id,
                RefundAmount = transaction.Amount,
                RefundStatus = RefundStatus.Pending,
                DisputeReason = reason,
                CreatedAt = DateTime.UtcNow
            };

            _context.Refunds.Add(refund);

            refundResponses.Add(new RefundResponse
            {
                RefundId = refund.Id,
                OrderId = order.Id,
                PaymentTransactionId = transaction.Id,
                RefundAmount = refund.RefundAmount,
                RefundStatus = refund.RefundStatus.ToString(),
                DisputeReason = refund.DisputeReason,
                CreatedAt = refund.CreatedAt,
                UpdatedAt = refund.CreatedAt
            });

            _logger.LogInformation(
                "Refund request created: RefundId={RefundId}, TransactionId={TransactionId}, Amount={Amount}",
                refund.Id, transaction.Id, refund.RefundAmount);
        }

        // Cancel remaining pending/processing transactions (if any)
        CancelPendingTransactions(order);

        return Result<List<RefundResponse>>.Success(refundResponses);
    }

    private static void SetOrderCancelled(Order order)
    {
        order.OrderStatus = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
    }

    private static void CancelPendingTransactions(Order order)
    {
        foreach (var transaction in order.PaymentTransactions
            .Where(t => t.TransactionStatus == PaymentStatus.Pending
                     || t.TransactionStatus == PaymentStatus.Processing))
        {
            transaction.TransactionStatus = PaymentStatus.Cancelled;
            transaction.UpdatedAt = DateTime.UtcNow;
        }
    }
}

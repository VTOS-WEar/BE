using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Orders.DTOs;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Orders.Commands;

/// <summary>
/// Interface for processing PayOS payment webhook
/// </summary>
public interface IPaymentWebhookHandler
{
    Task<Result<PaymentWebhookProcessResponse>> HandleWebhookAsync(PaymentWebhookResponse webhook, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handler for processing PayOS payment webhook
/// Updates transaction, order, wallet, and product variants on successful payment
/// </summary>
public class PaymentWebhookHandler : IPaymentWebhookHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<PaymentWebhookHandler> _logger;

    public PaymentWebhookHandler(IApplicationDbContext context, ILogger<PaymentWebhookHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<PaymentWebhookProcessResponse>> HandleWebhookAsync(
        PaymentWebhookResponse webhook,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Validate webhook
            if (webhook?.Data == null || string.IsNullOrEmpty(webhook.Data.PaymentLinkId))
            {
                _logger.LogWarning("Invalid webhook: missing PaymentLinkId");
                return Result<PaymentWebhookProcessResponse>.Failure("Invalid webhook data", "INVALID_WEBHOOK");
            }

            // Step 2: Find payment transaction by PaymentLinkId
            var paymentTransaction = await _context.PaymentTransactions
                .Include(pt => pt.Order)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.ProductVariant)
                .Include(pt => pt.Wallet)
                .FirstOrDefaultAsync(pt => pt.PaymentLinkId == webhook.Data.PaymentLinkId, cancellationToken);

            if (paymentTransaction == null)
            {
                _logger.LogWarning("Payment transaction not found for PaymentLinkId: {PaymentLinkId}", webhook.Data.PaymentLinkId);
                return Result<PaymentWebhookProcessResponse>.Failure("Payment transaction not found", "TRANSACTION_NOT_FOUND");
            }

            // Step 3: Check if transaction is already in final state
            if (paymentTransaction.TransactionStatus == PaymentStatus.Completed || 
                paymentTransaction.TransactionStatus == PaymentStatus.Cancelled)
            {
                _logger.LogInformation("Transaction {TransactionId} already in final state: {Status}", 
                    paymentTransaction.Id, paymentTransaction.TransactionStatus);
                return Result<PaymentWebhookProcessResponse>.Failure($"Transaction already processed with status: {paymentTransaction.TransactionStatus}", "TRANSACTION_ALREADY_PROCESSED");
            }

            // If webhook success = false, mark transaction as failed
            if (!webhook.Success)
            {
                paymentTransaction.TransactionStatus = PaymentStatus.Failed;
                paymentTransaction.TransactionLog = $"Payment failed: {webhook.Desc}";
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Payment failed for transaction {TransactionId}: {Reason}", paymentTransaction.Id, webhook.Desc);
                return Result<PaymentWebhookProcessResponse>.Failure("Payment failed, transaction updated", "PAYMENT_FAILED");
            }

            // Step 4: Process successful payment
            return await ProcessSuccessfulPaymentAsync(paymentTransaction, webhook, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment webhook");
            return Result<PaymentWebhookProcessResponse>.Failure($"Error processing webhook: {ex.Message}", "WEBHOOK_PROCESSING_ERROR");
        }
    }

    /// <summary>
    /// Process successful payment: update transaction, order, wallet, and product variants
    /// </summary>
    private async Task<Result<PaymentWebhookProcessResponse>> ProcessSuccessfulPaymentAsync(
        PaymentTransaction paymentTransaction,
        PaymentWebhookResponse webhook,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = paymentTransaction.Order;
            var wallet = paymentTransaction.Wallet;

            // Step 1: Update transaction status
            paymentTransaction.TransactionStatus = PaymentStatus.Completed;
            paymentTransaction.TransactionTimestamp = DateTime.UtcNow;
            paymentTransaction.TransactionLog = webhook.Data != null 
                ? $"Payment completed via {webhook.Data.CounterAccountBankName}: {webhook.Data.Reference}" 
                : "Payment completed";

            // Step 2: Update order status
            order.OrderStatus = OrderStatus.Paid;

            // Step 3: Update school wallet if exists
            if (wallet != null)
            {
                var previousBalance = wallet.Balance;
                wallet.Balance += paymentTransaction.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("Updated wallet {WalletId} balance from {PrevBalance} to {NewBalance}", 
                    wallet.Id, previousBalance, wallet.Balance);
            }

            // Step 4: Update product variants inventory (if applicable)
            if (order.OrderItems != null && order.OrderItems.Any())
            {
                foreach (var orderItem in order.OrderItems)
                {
                    if (orderItem.ProductVariant != null)
                    {
                        var previousStock = orderItem.ProductVariant.StockQuantity;
                        orderItem.ProductVariant.StockQuantity -= orderItem.Quantity;
                        
                        // Log warning if stock becomes negative
                        if (orderItem.ProductVariant.StockQuantity < 0)
                        {
                            _logger.LogWarning("ProductVariant {VariantId} stock became negative: {Stock}", 
                                orderItem.ProductVariant.Id, orderItem.ProductVariant.StockQuantity);
                        }
                        
                        _logger.LogInformation("Updated ProductVariant {VariantId} stock from {PrevStock} to {NewStock} (Order Qty: {OrderQty})", 
                            orderItem.ProductVariant.Id, previousStock, orderItem.ProductVariant.StockQuantity, orderItem.Quantity);
                    }
                }
            }

            // Step 5: Save all changes
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Payment webhook processed successfully: TransactionId={TransactionId}, OrderId={OrderId}", 
                paymentTransaction.Id, order.Id);

            return Result<PaymentWebhookProcessResponse>.Success(new PaymentWebhookProcessResponse("Payment processed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing successful payment for transaction {TransactionId}", paymentTransaction.Id);
            return Result<PaymentWebhookProcessResponse>.Failure($"Error processing successful payment: {ex.Message}", "PAYMENT_PROCESSING_ERROR");
        }
    }
}

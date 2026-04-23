using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Orders.DTOs;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Orders.Commands;

public interface IPaymentWebhookHandler
{
    Task<Result<PaymentWebhookProcessResponse>> HandleWebhookAsync(PaymentWebhookResponse webhook, CancellationToken cancellationToken = default);
}

public class PaymentWebhookHandler : IPaymentWebhookHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<PaymentWebhookHandler> _logger;
    private readonly IEmailService _emailService;

    public PaymentWebhookHandler(IApplicationDbContext context, ILogger<PaymentWebhookHandler> logger, IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<Result<PaymentWebhookProcessResponse>> HandleWebhookAsync(
        PaymentWebhookResponse webhook,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (webhook?.Data == null || string.IsNullOrEmpty(webhook.Data.PaymentLinkId))
            {
                _logger.LogWarning("Invalid webhook: missing PaymentLinkId");
                return Result<PaymentWebhookProcessResponse>.Failure("Invalid webhook data", "INVALID_WEBHOOK");
            }

            var paymentTransaction = await _context.PaymentTransactions
                .Include(pt => pt.Order)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.ProductVariant)
                .Include(pt => pt.Order)
                    .ThenInclude(o => o.ChildProfile)
                        .ThenInclude(cp => cp.ParentUser)
                .Include(pt => pt.Order)
                    .ThenInclude(o => o.Provider)
                .Include(pt => pt.Order)
                    .ThenInclude(o => o.SemesterPublication)
                .Include(pt => pt.Wallet)
                .FirstOrDefaultAsync(pt => pt.PaymentLinkId == webhook.Data.PaymentLinkId, cancellationToken);

            if (paymentTransaction == null)
            {
                _logger.LogWarning("Payment transaction not found for PaymentLinkId: {PaymentLinkId}", webhook.Data.PaymentLinkId);
                return Result<PaymentWebhookProcessResponse>.Failure("Payment transaction not found", "TRANSACTION_NOT_FOUND");
            }

            if (paymentTransaction.TransactionStatus == PaymentStatus.Completed ||
                paymentTransaction.TransactionStatus == PaymentStatus.Cancelled)
            {
                _logger.LogInformation(
                    "Transaction {TransactionId} already in final state: {Status}",
                    paymentTransaction.Id,
                    paymentTransaction.TransactionStatus);
                return Result<PaymentWebhookProcessResponse>.Failure(
                    $"Transaction already processed with status: {paymentTransaction.TransactionStatus}",
                    "TRANSACTION_ALREADY_PROCESSED");
            }

            if (!webhook.Success)
            {
                paymentTransaction.TransactionStatus = PaymentStatus.Failed;
                paymentTransaction.TransactionLog = $"Payment failed: {webhook.Desc}";
                await _context.SaveChangesAsync(cancellationToken);
                return Result<PaymentWebhookProcessResponse>.Failure("Payment failed, transaction updated", "PAYMENT_FAILED");
            }

            return await ProcessSuccessfulPaymentAsync(paymentTransaction, webhook, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment webhook");
            return Result<PaymentWebhookProcessResponse>.Failure($"Error processing webhook: {ex.Message}", "WEBHOOK_PROCESSING_ERROR");
        }
    }

    private async Task<Result<PaymentWebhookProcessResponse>> ProcessSuccessfulPaymentAsync(
        PaymentTransaction paymentTransaction,
        PaymentWebhookResponse webhook,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = paymentTransaction.Order;
            var wallet = paymentTransaction.Wallet ?? await ResolveSchoolWalletAsync(order, cancellationToken);

            paymentTransaction.WalletID = wallet?.Id;
            paymentTransaction.Wallet = wallet;
            paymentTransaction.TransactionStatus = PaymentStatus.Completed;
            paymentTransaction.TransactionTimestamp = DateTime.UtcNow;
            paymentTransaction.TransactionType = TransactionType.OrderPayment;
            paymentTransaction.EscrowStatus = EscrowStatus.Held;
            paymentTransaction.TransactionLog = webhook.Data != null
                ? $"Payment completed via {webhook.Data.CounterAccountBankName}: {webhook.Data.Reference}"
                : "Payment completed";

            order.OrderStatus = OrderStatus.Paid;

            if (wallet != null)
            {
                wallet.Balance += paymentTransaction.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;
            }

            if (order.OrderItems != null && order.OrderItems.Any())
            {
                foreach (var orderItem in order.OrderItems)
                {
                    if (orderItem.ProductVariant == null)
                        continue;

                    orderItem.ProductVariant.StockQuantity -= orderItem.Quantity;
                    if (orderItem.ProductVariant.StockQuantity < 0)
                    {
                        _logger.LogWarning(
                            "ProductVariant {VariantId} stock became negative: {Stock}",
                            orderItem.ProductVariant.Id,
                            orderItem.ProductVariant.StockQuantity);
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                var parent = order.ChildProfile?.ParentUser;
                if (parent != null && !string.IsNullOrEmpty(parent.Email))
                {
                    var orderCode = order.Id.ToString()[..8].ToUpper();
                    var orderContextLabel = BuildOrderContextLabel(order);
                    await _emailService.SendOrderConfirmationEmailAsync(
                        parent.Email, parent.FullName, orderCode,
                        order.TotalAmount, orderContextLabel, cancellationToken);
                }
            }
            catch (Exception emailEx)
            {
                _logger.LogWarning(emailEx, "Failed to send order confirmation email for Order {OrderId}", order.Id);
            }

            return Result<PaymentWebhookProcessResponse>.Success(new PaymentWebhookProcessResponse("Payment processed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing successful payment for transaction {TransactionId}", paymentTransaction.Id);
            return Result<PaymentWebhookProcessResponse>.Failure($"Error processing successful payment: {ex.Message}", "PAYMENT_PROCESSING_ERROR");
        }
    }

    private static string BuildOrderContextLabel(Order order)
    {
        var semesterLabel = order.SemesterPublication != null
            ? $"{order.SemesterPublication.Semester} {order.SemesterPublication.AcademicYear}"
            : null;

        if (!string.IsNullOrWhiteSpace(semesterLabel) && !string.IsNullOrWhiteSpace(order.Provider?.ProviderName))
        {
            return $"{semesterLabel} - {order.Provider.ProviderName}";
        }

        if (!string.IsNullOrWhiteSpace(semesterLabel))
        {
            return semesterLabel;
        }

        if (!string.IsNullOrWhiteSpace(order.Provider?.ProviderName))
        {
            return order.Provider.ProviderName;
        }

        return "VTOS Order";
    }
}

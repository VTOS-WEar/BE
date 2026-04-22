using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Common.Models;
using VTOS.Application.Common.Settings;
using VTOS.Application.Features.Orders.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Orders.Commands;

/// <summary>
/// Command to retry payment for a cancelled/failed order.
/// Creates a new PayOS payment link and a new PaymentTransaction,
/// then resets the order status back to Pending.
/// </summary>
public record RetryPaymentCommand(Guid ParentId, Guid OrderId);

public record RetryPaymentResponse(
    Guid OrderId,
    Guid PaymentTransactionId,
    decimal TotalAmount,
    string PaymentLink,
    int OrderCode
);

public interface IRetryPaymentCommandHandler
{
    Task<Result<RetryPaymentResponse>> HandleAsync(RetryPaymentCommand command, CancellationToken ct = default);
}

public class RetryPaymentCommandHandler : IRetryPaymentCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPayOSService _payOSService;
    private readonly ILogger<RetryPaymentCommandHandler> _logger;
    private readonly PaymentSettings _paymentSettings;

    public RetryPaymentCommandHandler(
        IApplicationDbContext context,
        IPayOSService payOSService,
        ILogger<RetryPaymentCommandHandler> logger,
        IOptions<PaymentSettings> paymentSettings)
    {
        _context = context;
        _payOSService = payOSService;
        _logger = logger;
        _paymentSettings = paymentSettings.Value;
    }

    public async Task<Result<RetryPaymentResponse>> HandleAsync(RetryPaymentCommand command, CancellationToken ct = default)
    {
        try
        {
            // 1. Find the order
            var order = await _context.Orders
                .Include(o => o.ChildProfile)
                .FirstOrDefaultAsync(o => o.Id == command.OrderId, ct);

            if (order == null)
                return Result<RetryPaymentResponse>.Failure("Order not found.", "ORDER_NOT_FOUND");

            // 2. Verify ownership: the order's child must belong to this parent
            var child = await _context.ChildProfiles.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == order.ChildProfileID, ct);
            if (child == null || child.ParentUserID != command.ParentId)
                return Result<RetryPaymentResponse>.Failure("Access denied.", "ACCESS_DENIED");

            // 3. Only allow retry for Cancelled or Pending orders
            if (order.OrderStatus != OrderStatus.Cancelled && order.OrderStatus != OrderStatus.Pending)
                return Result<RetryPaymentResponse>.Failure(
                    "Only cancelled or pending orders can be retried.",
                    "INVALID_STATUS");

            // 4. Cancel any existing pending payment transactions for this order
            var existingPendingPayments = await _context.PaymentTransactions
                .Where(pt => pt.OrderID == order.Id && pt.TransactionStatus == PaymentStatus.Pending)
                .ToListAsync(ct);

            foreach (var pt in existingPendingPayments)
            {
                pt.TransactionStatus = PaymentStatus.Cancelled;
                pt.TransactionLog = (pt.TransactionLog ?? "") + " | Cancelled for retry payment";

                // Try to cancel PayOS link if exists
                if (!string.IsNullOrEmpty(pt.PaymentLinkId))
                {
                    try
                    {
                        await _payOSService.CancelPaymentLinkAsync(pt.PaymentLinkId, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cancel old PayOS link {PaymentLinkId}", pt.PaymentLinkId);
                    }
                }
            }

            // 5. Generate new PayOS payment link
            var returnUrl = $"{_paymentSettings.ReturnBaseUrl}{_paymentSettings.ReturnSuccessPath}";
            var cancelUrl = $"{_paymentSettings.ReturnBaseUrl}{_paymentSettings.ReturnCancelPath}";

            var paymentLinkRequest = new CreatePaymentLinkRequest
            {
                Amount = (int)order.TotalAmount,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl,
            };

            var paymentLinkResponse = await _payOSService.CreatePayOSPaymentLinkAsync(paymentLinkRequest, ct);

            // 6. Get wallet ID from campaign if applicable
            Guid? schoolWalletId = null;
            if (order.CampaignID.HasValue && order.CampaignID != Guid.Empty)
            {
                schoolWalletId = await (
                    from c in _context.Campaigns
                    join w in _context.Wallets on c.SchoolID equals w.OwnerID
                    where c.Id == order.CampaignID.Value
                        && w.OwnerType == WalletOwnerType.School
                        && w.IsActive
                    select w.Id
                ).FirstOrDefaultAsync(ct);

                if (schoolWalletId == Guid.Empty)
                    schoolWalletId = null;
            }

            // 7. Create new PaymentTransaction
            var newPayment = new Domain.Entities.PaymentTransaction
            {
                Id = Guid.NewGuid(),
                OrderID = order.Id,
                WalletID = schoolWalletId,
                PaymentLinkId = paymentLinkResponse.PaymentLinkId,
                GatewayType = PaymentGatewayType.PayOS,
                TransactionType = TransactionType.OrderPayment,
                TransactionStatus = PaymentStatus.Pending,
                Amount = order.TotalAmount,
                TransactionTimestamp = DateTime.UtcNow,
                TransactionLog = "Retry payment transaction created",
                CreatedAt = DateTime.UtcNow
            };
            _context.PaymentTransactions.Add(newPayment);

            // 8. Reset order status to Pending
            order.OrderStatus = OrderStatus.Pending;

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Retry payment created: OrderId={OrderId}, NewPaymentId={PaymentId}",
                order.Id, newPayment.Id);

            return Result<RetryPaymentResponse>.Success(new RetryPaymentResponse(
                order.Id,
                newPayment.Id,
                order.TotalAmount,
                paymentLinkResponse.CheckoutUrl,
                paymentLinkResponse.OrderCode
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during retry payment for order {OrderId}", command.OrderId);
            return Result<RetryPaymentResponse>.Failure($"Retry payment failed: {ex.Message}", "RETRY_PAYMENT_ERROR");
        }
    }
}

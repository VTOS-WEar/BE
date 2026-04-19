using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Orders.Commands;

public record CancelPaymentTransactionCommand(Guid ParentId, Guid OrderId);

public interface ICancelPaymentTransactionCommandHandler
{
    Task<Result> HandleAsync(CancelPaymentTransactionCommand command, CancellationToken ct = default);
}

public class CancelPaymentTransactionCommandHandler : ICancelPaymentTransactionCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPayOSService _payOSService;
    private readonly ILogger<CancelPaymentTransactionCommandHandler> _logger;

    public CancelPaymentTransactionCommandHandler(
        IApplicationDbContext context,
        IPayOSService payOSService,
        ILogger<CancelPaymentTransactionCommandHandler> logger)
    {
        _context = context;
        _payOSService = payOSService;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(CancelPaymentTransactionCommand command, CancellationToken ct = default)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.ChildProfile)
                .FirstOrDefaultAsync(o => o.Id == command.OrderId, ct);

            if (order == null)
                return Result.Failure("Order not found.", "ORDER_NOT_FOUND");

            if (order.ChildProfile.ParentUserID != command.ParentId)
                return Result.Failure("Access denied.", "ACCESS_DENIED");

            var pendingPayments = await _context.PaymentTransactions
                .Where(pt => pt.OrderID == order.Id && pt.TransactionStatus == PaymentStatus.Pending)
                .ToListAsync(ct);

            foreach (var pt in pendingPayments)
            {
                pt.TransactionStatus = PaymentStatus.Cancelled;
                pt.TransactionLog = (pt.TransactionLog ?? "") + " | Cancelled by user return from PayOS";

                if (!string.IsNullOrEmpty(pt.PaymentLinkId))
                {
                    try
                    {
                        await _payOSService.CancelPaymentLinkAsync(pt.PaymentLinkId, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cancel PayOS link {PaymentLinkId} during cancel-transaction", pt.PaymentLinkId);
                    }
                }
            }

            // Do NOT change order.OrderStatus. Keep it Pending/whatever.
            await _context.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling transaction for order {OrderId}", command.OrderId);
            return Result.Failure("Failed to cancel payment transaction.", "CANCEL_TRANSACTION_ERROR");
        }
    }
}

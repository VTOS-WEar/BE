using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Payments.Commands;

// ── RefundOrderCommand ──────────────────────────────────────────────
// School initiates refund → wallet balance decreases (return to Parent)
public record RefundOrderCommand(Guid UserId, Guid OrderId, string? Reason);

public record RefundOrderResponse(Guid RefundId, decimal RefundAmount);

public interface IRefundOrderCommandHandler
{
    Task<Result<RefundOrderResponse>> HandleAsync(RefundOrderCommand command, CancellationToken ct = default);
}

public class RefundOrderCommandHandler : IRefundOrderCommandHandler
{
    private readonly IApplicationDbContext _db;

    public RefundOrderCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<RefundOrderResponse>> HandleAsync(RefundOrderCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null)
            return Result<RefundOrderResponse>.Failure("Access denied.", "ACCESS_DENIED");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        var order = await _db.Orders
            .Include(o => o.ChildProfile)
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, ct);

        if (order == null)
            return Result<RefundOrderResponse>.Failure("Order not found.", "ORDER_NOT_FOUND");

        var child = await _db.ChildProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == order.ChildProfileID, ct);
        if (child == null || child.SchoolID != schoolMgr?.SchoolID)
            return Result<RefundOrderResponse>.Failure("Access denied.", "ACCESS_DENIED");

        if (order.OrderStatus == OrderStatus.Pending || order.OrderStatus == OrderStatus.Cancelled || order.OrderStatus == OrderStatus.Refunded)
            return Result<RefundOrderResponse>.Failure("Order cannot be refunded in current status.", "INVALID_STATUS");

        // Find the original payment
        var originalPayment = order.PaymentTransactions
            .FirstOrDefault(p => p.TransactionType == TransactionType.OrderPayment && p.TransactionStatus == PaymentStatus.Completed);
        if (originalPayment == null)
            return Result<RefundOrderResponse>.Failure("No payment found for this order.", "NO_PAYMENT");

        // Deduct from wallet
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerID == schoolMgr.SchoolID && w.OwnerType == Domain.Enums.WalletOwnerType.School && w.IsActive, ct);
        if (wallet == null || wallet.Balance < order.TotalAmount)
            return Result<RefundOrderResponse>.Failure("Insufficient wallet balance for refund.", "INSUFFICIENT_BALANCE");

        wallet.Balance -= order.TotalAmount;
        wallet.UpdatedAt = DateTime.UtcNow;

        // Create refund transaction
        var refundTx = new Domain.Entities.PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderID = order.Id,
            WalletID = wallet.Id,
            TransactionType = TransactionType.Refund,
            GatewayType = PaymentGatewayType.Other,
            TransactionStatus = PaymentStatus.Completed,
            Amount = order.TotalAmount,
            TransactionTimestamp = DateTime.UtcNow,
            Description = $"Hoàn tiền đơn #{order.Id.ToString()[..8]}" + (string.IsNullOrEmpty(command.Reason) ? "" : $" - {command.Reason}"),
            CreatedAt = DateTime.UtcNow
        };
        _db.PaymentTransactions.Add(refundTx);

        // Create Refund record
        var refund = new Domain.Entities.Refund
        {
            Id = Guid.NewGuid(),
            PaymentID = originalPayment.Id,
            RefundAmount = order.TotalAmount,
            RefundStatus = RefundStatus.Completed,
            DisputeReason = command.Reason,
            CreatedAt = DateTime.UtcNow
        };
        _db.Refunds.Add(refund);

        order.OrderStatus = OrderStatus.Refunded;

        await _db.SaveChangesAsync(ct);

        return Result<RefundOrderResponse>.Success(
            new RefundOrderResponse(refund.Id, refund.RefundAmount));
    }
}

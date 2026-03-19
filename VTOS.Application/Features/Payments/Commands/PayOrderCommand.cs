using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Payments.Commands;

// ── PayOrderCommand ─────────────────────────────────────────────────
// Parent pays for their Order → money goes into School Wallet automatically
public record PayOrderCommand(Guid UserId, Guid OrderId);

public record PayOrderResponse(Guid PaymentId, decimal Amount, string Status);

public interface IPayOrderCommandHandler
{
    Task<Result<PayOrderResponse>> HandleAsync(PayOrderCommand command, CancellationToken ct = default);
}

public class PayOrderCommandHandler : IPayOrderCommandHandler
{
    private readonly IApplicationDbContext _db;

    public PayOrderCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PayOrderResponse>> HandleAsync(PayOrderCommand command, CancellationToken ct = default)
    {
        // 1. Find Order + verify ownership
        var order = await _db.Orders
            .Include(o => o.ChildProfile)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, ct);

        if (order == null)
            return Result<PayOrderResponse>.Failure("Order not found.", "ORDER_NOT_FOUND");

        // Verify the parent owns this order's child profile
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null)
            return Result<PayOrderResponse>.Failure("User not found.", "USER_NOT_FOUND");

        var child = await _db.ChildProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == order.ChildProfileID, ct);
        if (child == null || child.ParentUserID != command.UserId)
            return Result<PayOrderResponse>.Failure("Access denied.", "ACCESS_DENIED");

        if (order.OrderStatus != OrderStatus.Pending)
            return Result<PayOrderResponse>.Failure("Order is not in Pending status.", "INVALID_STATUS");

        // 2. Find Wallet for this order's school (via child profile -> school)
        var schoolId = child.SchoolID;
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerID == schoolId && w.OwnerType == Domain.Enums.WalletOwnerType.School && w.IsActive, ct);
        if (wallet == null)
        {
            // Auto-create wallet if not exists
            wallet = new Domain.Entities.Wallet
            {
                Id = Guid.NewGuid(),
                OwnerID = schoolId,
                OwnerType = Domain.Enums.WalletOwnerType.School,
                Balance = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Wallets.Add(wallet);
        }

        // 3. Create PaymentTransaction + fund wallet
        var payment = new Domain.Entities.PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderID = order.Id,
            WalletID = wallet.Id,
            TransactionType = TransactionType.OrderPayment,
            GatewayType = PaymentGatewayType.Other, // Internal payment
            TransactionStatus = PaymentStatus.Completed,
            Amount = order.TotalAmount,
            TransactionTimestamp = DateTime.UtcNow,
            Description = $"Thanh toán đơn hàng #{order.Id.ToString()[..8]}",
            CreatedAt = DateTime.UtcNow
        };
        _db.PaymentTransactions.Add(payment);

        // 4. Update wallet balance + order status
        wallet.Balance += order.TotalAmount;
        wallet.UpdatedAt = DateTime.UtcNow;
        order.OrderStatus = OrderStatus.Paid;

        await _db.SaveChangesAsync(ct);

        return Result<PayOrderResponse>.Success(
            new PayOrderResponse(payment.Id, payment.Amount, "Completed"));
    }
}

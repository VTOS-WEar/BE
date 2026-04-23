using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Payments.Commands;

public record PayProviderCommand(Guid UserId, Guid OrderId);

public record PayProviderResponse(Guid PaymentId, decimal Amount, string ProviderName);

public interface IPayProviderCommandHandler
{
    Task<Result<PayProviderResponse>> HandleAsync(PayProviderCommand command, CancellationToken ct = default);
}

public class PayProviderCommandHandler : IPayProviderCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IProviderPayoutService _providerPayoutService;

    public PayProviderCommandHandler(IApplicationDbContext db, IProviderPayoutService providerPayoutService)
    {
        _db = db;
        _providerPayoutService = providerPayoutService;
    }

    public async Task<Result<PayProviderResponse>> HandleAsync(PayProviderCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null)
            return Result<PayProviderResponse>.Failure("Access denied.", "ACCESS_DENIED");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        var order = await _db.Orders
            .Include(o => o.ChildProfile)
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, ct);

        if (order == null)
            return Result<PayProviderResponse>.Failure("Order not found.", "ORDER_NOT_FOUND");

        var child = await _db.ChildProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == order.ChildProfileID, ct);
        if (child == null || child.SchoolID != schoolMgr?.SchoolID)
            return Result<PayProviderResponse>.Failure("Order does not belong to your school.", "ACCESS_DENIED");

        if (order.OrderStatus != OrderStatus.Delivered)
            return Result<PayProviderResponse>.Failure("Order must be delivered before paying provider.", "INVALID_STATUS");

        var payoutResult = await _providerPayoutService.ReleaseOrderPayoutAsync(
            order.Id,
            DateTime.UtcNow,
            "Manual school-triggered provider payout.",
            requireDisputeWindow: false,
            ct);

        // 3. Find wallet and check balance
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerID == schoolMgr.SchoolID && w.OwnerType == Domain.Enums.WalletOwnerType.School && w.IsActive, ct);
        if (wallet == null || wallet.Balance < order.TotalAmount)
            return Result<PayProviderResponse>.Failure("Insufficient wallet balance.", "INSUFFICIENT_BALANCE");

        // 4. Deduct from wallet + create payment record
        var providerName = await _db.Providers
            .AsNoTracking()
            .Where(p => p.Id == order.ProviderID)
            .Select(p => p.ProviderName)
            .FirstOrDefaultAsync(ct) ?? "Provider";

        wallet.Balance -= order.TotalAmount;
        wallet.UpdatedAt = DateTime.UtcNow;

        var payment = new Domain.Entities.PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderID = order.Id,
            WalletID = wallet.Id,
            TransactionType = TransactionType.ProviderPayment,
            GatewayType = PaymentGatewayType.Other,
            TransactionStatus = PaymentStatus.Completed,
            Amount = order.TotalAmount,
            TransactionTimestamp = DateTime.UtcNow,
            Description = $"Thanh toán NCC: {providerName} - Đơn #{order.Id.ToString()[..8]}",
            CreatedAt = DateTime.UtcNow
        };
        _db.PaymentTransactions.Add(payment);

        order.IsProviderPaid = true;

        await _db.SaveChangesAsync(ct);

        return Result<PayProviderResponse>.Success(
            new PayProviderResponse(
                payoutResult.Value.PayoutRecordId,
                payoutResult.Value.Amount,
                payoutResult.Value.ProviderName));
    }
}

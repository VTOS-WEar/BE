using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Payments.Commands;

// ── PayProviderCommand ──────────────────────────────────────────────
// School pays Provider from wallet for a delivered order
public record PayProviderCommand(Guid UserId, Guid OrderId);

public record PayProviderResponse(Guid PaymentId, decimal Amount, string ProviderName);

public interface IPayProviderCommandHandler
{
    Task<Result<PayProviderResponse>> HandleAsync(PayProviderCommand command, CancellationToken ct = default);
}

public class PayProviderCommandHandler : IPayProviderCommandHandler
{
    private readonly IApplicationDbContext _db;

    public PayProviderCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PayProviderResponse>> HandleAsync(PayProviderCommand command, CancellationToken ct = default)
    {
        // 1. Verify school user
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null || user.SchoolID == null)
            return Result<PayProviderResponse>.Failure("Access denied.", "ACCESS_DENIED");

        // 2. Find Order and verify it's from this school
        var order = await _db.Orders
            .Include(o => o.ChildProfile)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.ProductVariant).ThenInclude(pv => pv.Outfit)
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, ct);

        if (order == null)
            return Result<PayProviderResponse>.Failure("Order not found.", "ORDER_NOT_FOUND");

        var child = await _db.ChildProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == order.ChildProfileID, ct);
        if (child == null || child.SchoolID != user.SchoolID)
            return Result<PayProviderResponse>.Failure("Order does not belong to your school.", "ACCESS_DENIED");

        if (order.OrderStatus != OrderStatus.Delivered && order.OrderStatus != OrderStatus.Shipped)
            return Result<PayProviderResponse>.Failure("Order must be delivered before paying provider.", "INVALID_STATUS");

        if (order.IsProviderPaid)
            return Result<PayProviderResponse>.Failure("Provider already paid for this order.", "ALREADY_PAID");

        // 3. Find wallet and check balance
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerID == user.SchoolID && w.OwnerType == Domain.Enums.WalletOwnerType.School && w.IsActive, ct);
        if (wallet == null || wallet.Balance < order.TotalAmount)
            return Result<PayProviderResponse>.Failure("Insufficient wallet balance.", "INSUFFICIENT_BALANCE");

        // 4. Deduct from wallet + create payment record
        var providerName = "Provider";
        // Try to find provider from campaign outfits
        if (order.CampaignID != null)
        {
            var campOutfit = await _db.CampaignOutfits
                .Include(co => co.Provider)
                .AsNoTracking()
                .FirstOrDefaultAsync(co => co.CampaignID == order.CampaignID, ct);
            if (campOutfit?.Provider != null)
                providerName = campOutfit.Provider.ProviderName;
        }

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
            new PayProviderResponse(payment.Id, payment.Amount, providerName));
    }
}

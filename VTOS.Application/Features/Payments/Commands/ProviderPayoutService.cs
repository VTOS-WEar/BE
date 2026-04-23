using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Payments.Commands;

public record ProviderPayoutResult(
    Guid PayoutRecordId,
    Guid ProviderWalletId,
    decimal Amount,
    string ProviderName);

public interface IProviderPayoutService
{
    Task<Result<ProviderPayoutResult>> ReleaseOrderPayoutAsync(
        Guid orderId,
        DateTime processedAtUtc,
        string note,
        bool requireDisputeWindow,
        CancellationToken ct = default);
}

public class ProviderPayoutService : IProviderPayoutService
{
    private static readonly TimeSpan DisputeWindow = TimeSpan.FromDays(7);
    private readonly IApplicationDbContext _db;
    private readonly IOrderPaymentResolutionService _paymentResolutionService;

    public ProviderPayoutService(IApplicationDbContext db, IOrderPaymentResolutionService paymentResolutionService)
    {
        _db = db;
        _paymentResolutionService = paymentResolutionService;
    }

    public async Task<Result<ProviderPayoutResult>> ReleaseOrderPayoutAsync(
        Guid orderId,
        DateTime processedAtUtc,
        string note,
        bool requireDisputeWindow,
        CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.ChildProfile)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant)
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null)
            return Result<ProviderPayoutResult>.Failure("Order not found.", "ORDER_NOT_FOUND");

        if (order.OrderStatus != OrderStatus.Delivered)
            return Result<ProviderPayoutResult>.Failure("Order must be delivered before payout release.", "INVALID_ORDER_STATUS");

        if (order.IsProviderPaid)
            return Result<ProviderPayoutResult>.Failure("Provider already paid for this order.", "ALREADY_PAID");

        if (requireDisputeWindow && !HasSatisfiedDisputeWindow(order, processedAtUtc))
            return Result<ProviderPayoutResult>.Failure("Order is still inside the dispute window.", "DISPUTE_WINDOW_ACTIVE");

        var escrowPayment = order.PaymentTransactions
            .OrderByDescending(pt => pt.TransactionTimestamp)
            .FirstOrDefault(pt =>
                pt.TransactionType == TransactionType.OrderPayment &&
                pt.TransactionStatus == PaymentStatus.Completed);

        if (escrowPayment == null)
            return Result<ProviderPayoutResult>.Failure("Completed order payment not found.", "PAYMENT_NOT_FOUND");

        if (escrowPayment.EscrowStatus == EscrowStatus.Released || escrowPayment.PayoutRecordID.HasValue)
            return Result<ProviderPayoutResult>.Failure("Escrow already released for this order.", "ESCROW_ALREADY_RELEASED");

        var providerResolution = await _paymentResolutionService.ResolveProviderAsync(order, ct);
        if (!providerResolution.IsSuccess)
            return Result<ProviderPayoutResult>.Failure(providerResolution.Error!, providerResolution.ErrorCode);

        var (providerId, providerName) = providerResolution.Value;

        var schoolWallet = await _paymentResolutionService.ResolveSchoolWalletAsync(order, ct);
        if (!schoolWallet.IsSuccess)
            return Result<ProviderPayoutResult>.Failure(schoolWallet.Error!, schoolWallet.ErrorCode);

        var schoolWalletEntity = schoolWallet.Value!;
        if (schoolWalletEntity.Balance < order.TotalAmount)
            return Result<ProviderPayoutResult>.Failure("School wallet balance is insufficient for payout release.", "INSUFFICIENT_BALANCE");

        var providerWallet = await _paymentResolutionService.GetOrCreateProviderWalletAsync(providerId, processedAtUtc, ct);

        var payoutRecord = new PayoutRecord
        {
            Id = Guid.NewGuid(),
            ProviderID = providerId,
            OrderID = order.Id,
            Amount = order.TotalAmount,
            Status = "Completed",
            PayoutMethod = "SystemCredits",
            ProcessedAt = processedAtUtc,
            AdminNote = note,
            CreatedAt = processedAtUtc
        };
        _db.PayoutRecords.Add(payoutRecord);

        schoolWalletEntity.Balance -= order.TotalAmount;
        schoolWalletEntity.UpdatedAt = processedAtUtc;

        providerWallet.Balance += order.TotalAmount;
        providerWallet.UpdatedAt = processedAtUtc;

        escrowPayment.EscrowStatus = EscrowStatus.Released;
        escrowPayment.PayoutRecordID = payoutRecord.Id;
        escrowPayment.UpdatedAt = processedAtUtc;

        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderID = order.Id,
            WalletID = schoolWalletEntity.Id,
            PayoutRecordID = payoutRecord.Id,
            TransactionType = TransactionType.EscrowRelease,
            GatewayType = PaymentGatewayType.Other,
            TransactionStatus = PaymentStatus.Completed,
            EscrowStatus = EscrowStatus.Released,
            Amount = order.TotalAmount,
            TransactionTimestamp = processedAtUtc,
            Description = $"Escrow released for order #{order.Id.ToString()[..8]}",
            TransactionLog = note,
            CreatedAt = processedAtUtc
        });

        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderID = order.Id,
            WalletID = providerWallet.Id,
            PayoutRecordID = payoutRecord.Id,
            TransactionType = TransactionType.ProviderPayout,
            GatewayType = PaymentGatewayType.Other,
            TransactionStatus = PaymentStatus.Completed,
            EscrowStatus = EscrowStatus.Released,
            Amount = order.TotalAmount,
            TransactionTimestamp = processedAtUtc,
            Description = $"Provider payout for order #{order.Id.ToString()[..8]}",
            TransactionLog = note,
            CreatedAt = processedAtUtc
        });

        order.IsProviderPaid = true;
        order.ProviderID ??= providerId;
        order.UpdatedAt = processedAtUtc;

        await _db.SaveChangesAsync(ct);

        return Result<ProviderPayoutResult>.Success(new ProviderPayoutResult(
            payoutRecord.Id,
            providerWallet.Id,
            order.TotalAmount,
            providerName));
    }

    private static bool HasSatisfiedDisputeWindow(Order order, DateTime processedAtUtc)
    {
        var deliveredAt = order.UpdatedAt ?? order.OrderDate;
        return deliveredAt <= processedAtUtc.Subtract(DisputeWindow);
    }

}

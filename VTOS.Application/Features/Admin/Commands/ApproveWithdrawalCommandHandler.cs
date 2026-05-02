using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Notifications;
using VTOS.Application.Features.Schools.Commands;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Commands;

public class ApproveWithdrawalCommandHandler : IApproveWithdrawalCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<ApproveWithdrawalCommandHandler> _logger;
    private readonly INotificationService _notificationService;

    public ApproveWithdrawalCommandHandler(
        IApplicationDbContext db,
        ILogger<ApproveWithdrawalCommandHandler> logger,
        INotificationService notificationService)
    {
        _db = db;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<Result<WithdrawalRequestResponse>> HandleAsync(ApproveWithdrawalCommand command, CancellationToken ct = default)
    {
        var withdrawal = await _db.Set<WalletWithdrawalRequest>()
            .Include(w => w.Wallet)
            .FirstOrDefaultAsync(w => w.Id == command.WithdrawalRequestId, ct);

        if (withdrawal == null)
            return Result<WithdrawalRequestResponse>.Failure("Withdrawal request not found.", "WITHDRAWAL_NOT_FOUND");

        if (withdrawal.Status != "Pending")
            return Result<WithdrawalRequestResponse>.Failure($"Withdrawal request cannot be approved. Current status: {withdrawal.Status}", "WITHDRAWAL_NOT_APPROVABLE");

        var wallet = withdrawal.Wallet;
        if (wallet.Balance < withdrawal.Amount)
            return Result<WithdrawalRequestResponse>.Failure("Insufficient wallet balance.", "INSUFFICIENT_BALANCE");

        var now = DateTime.UtcNow;

        wallet.Balance -= withdrawal.Amount;
        wallet.UpdatedAt = now;

        withdrawal.Status = "Approved";
        withdrawal.ApprovedAt = now;
        withdrawal.AdminNote = command.AdminNote;

        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            WalletID = wallet.Id,
            TransactionType = TransactionType.ProviderWithdrawal,
            GatewayType = PaymentGatewayType.Other,
            TransactionStatus = PaymentStatus.Completed,
            Amount = withdrawal.NetAmount,
            TransactionTimestamp = now,
            Description = $"Provider withdrawal (net) for request #{withdrawal.Id.ToString()[..8]}",
            CreatedAt = now
        });

        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            WalletID = null,
            TransactionType = TransactionType.ProviderWithdrawalFee,
            GatewayType = PaymentGatewayType.Other,
            TransactionStatus = PaymentStatus.Completed,
            Amount = withdrawal.FeeAmount,
            TransactionTimestamp = now,
            Description = $"Platform withdrawal fee (2%) for request #{withdrawal.Id.ToString()[..8]}",
            CreatedAt = now
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Withdrawal approved: WithdrawalId={WithdrawalId}, Amount={Amount}, WalletId={WalletId}",
            withdrawal.Id, withdrawal.Amount, wallet.Id);

        try
        {
            if (wallet.OwnerType == WalletOwnerType.Provider)
            {
                await _notificationService.NotifyProviderAsync(
                    wallet.OwnerID,
                    "Withdrawal approved",
                    $"Withdrawal request for {withdrawal.Amount:N0} VND has been approved.",
                    "Withdrawal",
                    withdrawal.Id,
                    "WithdrawalRequest",
                    "/provider/wallet",
                    ct);
            }
            else if (wallet.OwnerType == WalletOwnerType.Parent)
            {
                await _notificationService.CreateAsync(
                    wallet.OwnerID,
                    "Withdrawal approved",
                    $"Withdrawal request for {withdrawal.Amount:N0} VND has been approved.",
                    "Withdrawal",
                    withdrawal.Id,
                    "WithdrawalRequest",
                    "/parentprofile/wallet",
                    ct);
            }
        }
        catch
        {
            // Notification failure should not block approval.
        }

        return Result<WithdrawalRequestResponse>.Success(new WithdrawalRequestResponse
        {
            WithdrawalRequestId = withdrawal.Id,
            WalletId = wallet.Id,
            Amount = withdrawal.Amount,
            FeeRate = withdrawal.FeeRate,
            FeeAmount = withdrawal.FeeAmount,
            NetAmount = withdrawal.NetAmount,
            Status = withdrawal.Status,
            RequestedAt = withdrawal.RequestedAt,
            ApprovedAt = withdrawal.ApprovedAt,
            AdminNote = withdrawal.AdminNote
        });
    }
}

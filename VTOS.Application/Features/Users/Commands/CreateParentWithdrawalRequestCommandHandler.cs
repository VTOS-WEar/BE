using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Notifications;
using VTOS.Application.Features.Schools.Commands;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Users.Commands;

public class CreateParentWithdrawalRequestCommandHandler : ICreateParentWithdrawalRequestCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<CreateParentWithdrawalRequestCommandHandler> _logger;
    private readonly INotificationService _notificationService;

    public CreateParentWithdrawalRequestCommandHandler(
        IApplicationDbContext db,
        ILogger<CreateParentWithdrawalRequestCommandHandler> logger,
        INotificationService notificationService)
    {
        _db = db;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<Result<WithdrawalRequestResponse>> HandleAsync(CreateParentWithdrawalRequestCommand command, CancellationToken ct = default)
    {
        var parentUser = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == command.ParentUserId, ct);

        if (parentUser == null)
            return Result<WithdrawalRequestResponse>.Failure("User not found.", "USER_NOT_FOUND");

        if (parentUser.Role?.RoleName != "Parent")
            return Result<WithdrawalRequestResponse>.Failure("Only parents can create withdrawal requests.", "FORBIDDEN");

        if (command.Amount <= 0)
            return Result<WithdrawalRequestResponse>.Failure("Withdrawal amount must be greater than zero.", "INVALID_AMOUNT");

        var wallet = await _db.Wallets
            .FirstOrDefaultAsync(w => w.OwnerID == command.ParentUserId && w.OwnerType == WalletOwnerType.Parent && w.IsActive, ct);

        if (wallet == null)
        {
            wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                OwnerID = command.ParentUserId,
                OwnerType = WalletOwnerType.Parent,
                Balance = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Wallets.Add(wallet);
        }

        if (wallet.Balance < command.Amount)
            return Result<WithdrawalRequestResponse>.Failure("Insufficient wallet balance.", "INSUFFICIENT_BALANCE");

        var parentBank = await _db.ParentBankAccounts
            .FirstOrDefaultAsync(b => b.ParentUserID == command.ParentUserId && b.IsDefault, ct);

        if (parentBank == null)
            return Result<WithdrawalRequestResponse>.Failure("Parent default bank account is not configured.", "BANK_NOT_CONFIGURED");

        if (string.IsNullOrWhiteSpace(parentBank.BankCode) || string.IsNullOrWhiteSpace(parentBank.AccountNumber))
            return Result<WithdrawalRequestResponse>.Failure("Parent bank account is missing bank code or account number.", "BANK_NOT_CONFIGURED");

        var hasPending = await _db.WalletWithdrawalRequests
            .AnyAsync(w => w.WalletID == wallet.Id && w.Status == "Pending", ct);

        if (hasPending)
            return Result<WithdrawalRequestResponse>.Failure("There is already a pending withdrawal request.", "PENDING_EXISTS");

        wallet.BankCode = parentBank.BankCode;
        wallet.BankName = parentBank.BankName;
        wallet.BankAccountNumber = parentBank.AccountNumber;
        wallet.BankAccountName = parentBank.AccountHolderName;
        wallet.UpdatedAt = DateTime.UtcNow;

        var withdrawalRequest = new WalletWithdrawalRequest
        {
            Id = Guid.NewGuid(),
            WalletID = wallet.Id,
            Amount = command.Amount,
            Status = "Pending",
            RequestedAt = DateTime.UtcNow
        };

        _db.WalletWithdrawalRequests.Add(withdrawalRequest);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Parent withdrawal request created: Id={Id}, WalletId={WalletId}, Amount={Amount}",
            withdrawalRequest.Id, wallet.Id, command.Amount);

        try
        {
            await _notificationService.NotifyAdminsAsync(
                "New parent withdrawal request",
                $"{parentUser.FullName ?? "Parent"} requested withdrawal of {command.Amount:N0} VND.",
                "WithdrawalRequest",
                withdrawalRequest.Id,
                "WithdrawalRequest",
                "/admin/withdrawals",
                ct);
        }
        catch
        {
            // Notification failure should not block withdrawal creation.
        }

        return Result<WithdrawalRequestResponse>.Success(new WithdrawalRequestResponse
        {
            WithdrawalRequestId = withdrawalRequest.Id,
            WalletId = wallet.Id,
            Amount = withdrawalRequest.Amount,
            Status = withdrawalRequest.Status,
            RequestedAt = withdrawalRequest.RequestedAt
        });
    }
}

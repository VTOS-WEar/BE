using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.Commands;
using VTOS.Domain.Entities;
using VTOS.Application.Features.Notifications;

namespace VTOS.Application.Features.Providers.Commands;

public class CreateProviderWithdrawalRequestCommandHandler : ICreateProviderWithdrawalRequestCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<CreateProviderWithdrawalRequestCommandHandler> _logger;
    private readonly INotificationService _notificationService;

    public CreateProviderWithdrawalRequestCommandHandler(
        IApplicationDbContext db,
        ILogger<CreateProviderWithdrawalRequestCommandHandler> logger,
        INotificationService notificationService)
    {
        _db = db;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<Result<WithdrawalRequestResponse>> HandleAsync(CreateProviderWithdrawalRequestCommand command, CancellationToken ct = default)
    {
        // Step 1: Validate user is Provider role
        var providerUser = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == command.ProviderUserId, ct);

        if (providerUser == null)
            return Result<WithdrawalRequestResponse>.Failure("User not found.", "USER_NOT_FOUND");

        if (providerUser.Role?.RoleName != "Provider")
            return Result<WithdrawalRequestResponse>.Failure("Only providers can create withdrawal requests.", "FORBIDDEN");

        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == providerUser.Id, ct);
        if (providerMgr == null)
            return Result<WithdrawalRequestResponse>.Failure("User is not assigned to any provider.", "PROVIDER_NOT_FOUND");

        // Step 2: Load provider wallet
        var wallet = await _db.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.OwnerID == providerMgr.ProviderID && w.OwnerType == Domain.Enums.WalletOwnerType.Provider && w.IsActive, ct);

        if (wallet == null)
            return Result<WithdrawalRequestResponse>.Failure("Provider wallet not found or inactive.", "WALLET_NOT_FOUND");

        // Step 3: Validate amount
        if (command.Amount <= 0)
            return Result<WithdrawalRequestResponse>.Failure("Withdrawal amount must be greater than zero.", "INVALID_AMOUNT");

        if (wallet.Balance < command.Amount)
            return Result<WithdrawalRequestResponse>.Failure("Insufficient wallet balance.", "INSUFFICIENT_BALANCE");

        // Step 4: Validate bank account is configured
        if (string.IsNullOrWhiteSpace(wallet.BankCode) || string.IsNullOrWhiteSpace(wallet.BankAccountNumber))
            return Result<WithdrawalRequestResponse>.Failure("Provider bank account is not configured. Please update your bank information first.", "BANK_NOT_CONFIGURED");

        // Step 5: Check for existing pending withdrawal
        var hasPending = await _db.Set<WalletWithdrawalRequest>()
            .AnyAsync(w => w.WalletID == wallet.Id && w.Status == "Pending", ct);

        if (hasPending)
            return Result<WithdrawalRequestResponse>.Failure("There is already a pending withdrawal request.", "PENDING_EXISTS");

        // Step 6: Create withdrawal request
        var withdrawalRequest = new WalletWithdrawalRequest
        {
            Id = Guid.NewGuid(),
            WalletID = wallet.Id,
            Amount = command.Amount,
            Status = "Pending",
            RequestedAt = DateTime.UtcNow
        };

        _db.Set<WalletWithdrawalRequest>().Add(withdrawalRequest);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Provider withdrawal request created: Id={Id}, WalletId={WalletId}, Amount={Amount}",
            withdrawalRequest.Id, wallet.Id, command.Amount);

        // Notify admins
        try
        {
            var provider = await _db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == providerMgr.ProviderID, ct);
            await _notificationService.NotifyAdminsAsync(
                "💸 Yêu cầu rút tiền mới (NCC)",
                $"{provider?.ProviderName ?? "NCC"} yêu cầu rút {command.Amount:N0}đ.",
                "WithdrawalRequest",
                withdrawalRequest.Id, "WithdrawalRequest",
                "/admin/withdrawals", ct);
        }
        catch { /* Don't fail the main operation */ }

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

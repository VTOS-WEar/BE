using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Schools.Commands;

public class CreateWithdrawalRequestCommandHandler : ICreateWithdrawalRequestCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<CreateWithdrawalRequestCommandHandler> _logger;

    public CreateWithdrawalRequestCommandHandler(
        IApplicationDbContext db,
        ILogger<CreateWithdrawalRequestCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<WithdrawalRequestResponse>> HandleAsync(CreateWithdrawalRequestCommand command, CancellationToken ct = default)
    {
        // Step 1: Validate user is School role
        var schoolUser = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == command.SchoolUserId, ct);

        if (schoolUser == null)
            return Result<WithdrawalRequestResponse>.Failure("User not found.", "USER_NOT_FOUND");

        if (schoolUser.Role?.RoleName != "School")
            return Result<WithdrawalRequestResponse>.Failure("Only school managers can create withdrawal requests.", "FORBIDDEN");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == schoolUser.Id, ct);
        if (schoolMgr == null)
            return Result<WithdrawalRequestResponse>.Failure("User is not assigned to any school.", "SCHOOL_NOT_FOUND");

        // Step 2: Load school wallet
        var wallet = await _db.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.OwnerID == schoolMgr.SchoolID && w.OwnerType == Domain.Enums.WalletOwnerType.School && w.IsActive, ct);

        if (wallet == null)
            return Result<WithdrawalRequestResponse>.Failure("School wallet not found or inactive.", "WALLET_NOT_FOUND");

        // Step 3: Validate amount
        if (command.Amount <= 0)
            return Result<WithdrawalRequestResponse>.Failure("Withdrawal amount must be greater than zero.", "INVALID_AMOUNT");

        if (wallet.Balance < command.Amount)
            return Result<WithdrawalRequestResponse>.Failure("Insufficient wallet balance.", "INSUFFICIENT_BALANCE");

        // Step 4: Validate bank account is configured
        if (string.IsNullOrWhiteSpace(wallet.BankCode) || string.IsNullOrWhiteSpace(wallet.BankAccountNumber))
            return Result<WithdrawalRequestResponse>.Failure("School bank account is not configured. Please update your bank information first.", "BANK_NOT_CONFIGURED");

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
            "Withdrawal request created: Id={Id}, WalletId={WalletId}, Amount={Amount}",
            withdrawalRequest.Id, wallet.Id, command.Amount);

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

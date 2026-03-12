using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Schools.Commands;

public class UpdateSchoolBankAccountCommandHandler : IUpdateSchoolBankAccountCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<UpdateSchoolBankAccountCommandHandler> _logger;

    public UpdateSchoolBankAccountCommandHandler(
        IApplicationDbContext db,
        ILogger<UpdateSchoolBankAccountCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<SchoolBankAccountResponse>> HandleAsync(UpdateSchoolBankAccountCommand command, CancellationToken ct = default)
    {
        // Step 1: Validate user is School role
        var schoolUser = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == command.SchoolUserId, ct);

        if (schoolUser == null)
            return Result<SchoolBankAccountResponse>.Failure("User not found.", "USER_NOT_FOUND");

        if (schoolUser.Role?.RoleName != "School")
            return Result<SchoolBankAccountResponse>.Failure("Only school managers can update bank account.", "FORBIDDEN");

        if (schoolUser.SchoolID == null)
            return Result<SchoolBankAccountResponse>.Failure("User is not assigned to any school.", "SCHOOL_NOT_FOUND");

        // Step 2: Load school wallet
        var wallet = await _db.Set<SchoolWallet>()
            .FirstOrDefaultAsync(w => w.SchoolID == schoolUser.SchoolID.Value && w.IsActive, ct);

        if (wallet == null)
            return Result<SchoolBankAccountResponse>.Failure("School wallet not found or inactive.", "WALLET_NOT_FOUND");

        // Step 3: Partial update - only update fields that are provided
        if (command.BankCode is not null)
            wallet.BankCode = command.BankCode;
        if (command.BankName is not null)
            wallet.BankName = command.BankName;
        if (command.BankAccountNumber is not null)
            wallet.BankAccountNumber = command.BankAccountNumber;
        if (command.BankAccountName is not null)
            wallet.BankAccountName = command.BankAccountName;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "School bank account updated: WalletId={WalletId}, SchoolId={SchoolId}",
            wallet.Id, schoolUser.SchoolID.Value);

        return Result<SchoolBankAccountResponse>.Success(new SchoolBankAccountResponse
        {
            WalletId = wallet.Id,
            BankCode = wallet.BankCode,
            BankName = wallet.BankName,
            BankAccountNumber = wallet.BankAccountNumber,
            BankAccountName = wallet.BankAccountName
        });
    }
}

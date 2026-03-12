using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Users.Commands;

public class AddParentBankAccountCommandHandler : IAddParentBankAccountCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<AddParentBankAccountCommandHandler> _logger;

    public AddParentBankAccountCommandHandler(
        IApplicationDbContext db,
        ILogger<AddParentBankAccountCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<ParentBankAccountResponse>> HandleAsync(AddParentBankAccountCommand command, CancellationToken ct = default)
    {
        // Step 1: Validate user exists and is Parent role
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == command.ParentUserId, ct);

        if (user == null)
            return Result<ParentBankAccountResponse>.Failure("User not found.", "USER_NOT_FOUND");

        if (user.Role?.RoleName != "Parent")
            return Result<ParentBankAccountResponse>.Failure("Only parents can add bank accounts.", "FORBIDDEN");

        // Step 2: If IsDefault, unset existing default
        if (command.IsDefault)
        {
            var existingDefaults = await _db.ParentBankAccounts
                .Where(b => b.ParentUserID == command.ParentUserId && b.IsDefault)
                .ToListAsync(ct);

            foreach (var existing in existingDefaults)
                existing.IsDefault = false;
        }

        // Step 3: Create new bank account
        var bankAccount = new ParentBankAccount
        {
            Id = Guid.NewGuid(),
            ParentUserID = command.ParentUserId,
            BankName = command.BankName,
            BankCode = command.BankCode,
            AccountNumber = command.AccountNumber,
            AccountHolderName = command.AccountHolderName,
            IsDefault = command.IsDefault,
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.ParentBankAccounts.Add(bankAccount);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Parent bank account added: Id={Id}, ParentUserId={ParentUserId}",
            bankAccount.Id, command.ParentUserId);

        return Result<ParentBankAccountResponse>.Success(new ParentBankAccountResponse
        {
            BankAccountId = bankAccount.Id,
            BankName = bankAccount.BankName,
            BankCode = bankAccount.BankCode,
            AccountNumber = bankAccount.AccountNumber,
            AccountHolderName = bankAccount.AccountHolderName,
            IsDefault = bankAccount.IsDefault,
            IsVerified = bankAccount.IsVerified
        });
    }
}

using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Payments.Queries;

public record GetParentWalletQuery(Guid UserId);

public interface IGetParentWalletQueryHandler
{
    Task<Result<WalletDto>> HandleAsync(GetParentWalletQuery query, CancellationToken ct = default);
}

public class GetParentWalletQueryHandler : IGetParentWalletQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetParentWalletQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<WalletDto>> HandleAsync(GetParentWalletQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == query.UserId, ct);

        if (user == null || user.Role?.RoleName != "Parent")
            return Result<WalletDto>.Failure("Access denied.", "ACCESS_DENIED");

        var wallet = await _db.Wallets
            .FirstOrDefaultAsync(w => w.OwnerID == query.UserId && w.OwnerType == WalletOwnerType.Parent && w.IsActive, ct);

        if (wallet == null)
        {
            wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                OwnerID = query.UserId,
                OwnerType = WalletOwnerType.Parent,
                Balance = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Wallets.Add(wallet);
            await _db.SaveChangesAsync(ct);
        }

        var defaultBank = await _db.ParentBankAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.ParentUserID == query.UserId && b.IsDefault, ct);

        return Result<WalletDto>.Success(new WalletDto(
            wallet.Id,
            wallet.Balance,
            defaultBank?.BankCode ?? wallet.BankCode,
            defaultBank?.BankName ?? wallet.BankName,
            defaultBank?.AccountNumber ?? wallet.BankAccountNumber,
            defaultBank?.AccountHolderName ?? wallet.BankAccountName,
            wallet.IsActive,
            wallet.UpdatedAt));
    }
}

using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Payments.Queries;

// ── GetSchoolWalletQuery ────────────────────────────────────────────
public record GetSchoolWalletQuery(Guid UserId);

public record WalletDto(
    Guid WalletId,
    decimal Balance,
    string? BankCode,
    string? BankName,
    string? BankAccountNumber,
    string? BankAccountName,
    bool IsActive,
    DateTime UpdatedAt
);

public interface IGetSchoolWalletQueryHandler
{
    Task<Result<WalletDto>> HandleAsync(GetSchoolWalletQuery query, CancellationToken ct = default);
}

public class GetSchoolWalletQueryHandler : IGetSchoolWalletQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetSchoolWalletQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<WalletDto>> HandleAsync(GetSchoolWalletQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user == null || user.SchoolID == null)
            return Result<WalletDto>.Failure("Access denied.", "ACCESS_DENIED");

        var wallet = await _db.Wallets.AsNoTracking()
            .FirstOrDefaultAsync(w => w.OwnerID == user.SchoolID && w.OwnerType == Domain.Enums.WalletOwnerType.School && w.IsActive, ct);

        if (wallet == null)
        {
            // Auto-create wallet for schools that were approved before wallet auto-creation was added
            wallet = new Domain.Entities.Wallet
            {
                Id = Guid.NewGuid(),
                OwnerID = user.SchoolID.Value,
                OwnerType = Domain.Enums.WalletOwnerType.School,
                Balance = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Wallets.Add(wallet);
            await _db.SaveChangesAsync(ct);
        }

        return Result<WalletDto>.Success(new WalletDto(
            wallet.Id, wallet.Balance,
            wallet.BankCode, wallet.BankName,
            wallet.BankAccountNumber, wallet.BankAccountName,
            wallet.IsActive, wallet.UpdatedAt));
    }
}

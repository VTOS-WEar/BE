using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;

namespace VTOS.Application.Features.Admin.Commands;

public class ApproveUserCommandHandler : IApproveUserCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IUserStatusBroadcaster _broadcaster;

    public ApproveUserCommandHandler(IApplicationDbContext context, IUserStatusBroadcaster broadcaster)
    {
        _context = context;
        _broadcaster = broadcaster;
    }

    public async Task<bool> HandleAsync(
        ApproveUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user == null)
            return false;


        var schoolMgr = await _context.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, cancellationToken);


        var providerMgr = await _context.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, cancellationToken);


        user.IsActive = true;

        // Auto-create wallet for School manager
        if (schoolMgr?.SchoolID != null)
        {
            var existingWallet = await _context.Wallets
                .AnyAsync(w => w.OwnerID == schoolMgr!.SchoolID && w.OwnerType == Domain.Enums.WalletOwnerType.School, cancellationToken);
            if (!existingWallet)
            {
                _context.Wallets.Add(new Domain.Entities.Wallet
                {
                    Id = Guid.NewGuid(),
                    OwnerID = schoolMgr.SchoolID,
                    OwnerType = Domain.Enums.WalletOwnerType.School,
                    Balance = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
        // Auto-create wallet for Provider manager
        else if (providerMgr != null)
        {
            var existingWallet = await _context.Wallets
                .AnyAsync(w => w.OwnerID == providerMgr!.ProviderID && w.OwnerType == Domain.Enums.WalletOwnerType.Provider, cancellationToken);
            if (!existingWallet)
            {
                _context.Wallets.Add(new Domain.Entities.Wallet
                {
                    Id = Guid.NewGuid(),
                    OwnerID = providerMgr.ProviderID,
                    OwnerType = Domain.Enums.WalletOwnerType.Provider,
                    Balance = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Broadcast: all connected admin clients update their user status badge immediately
        await _broadcaster.BroadcastUserStatusChangedAsync(user.Id, isActive: true, cancellationToken);

        return true;
    }
}

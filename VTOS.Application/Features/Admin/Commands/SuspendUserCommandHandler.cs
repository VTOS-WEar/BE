using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;

namespace VTOS.Application.Features.Admin.Commands;

public class SuspendUserCommandHandler : ISuspendUserCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IUserStatusBroadcaster _broadcaster;

    public SuspendUserCommandHandler(IApplicationDbContext context, IUserStatusBroadcaster broadcaster)
    {
        _context = context;
        _broadcaster = broadcaster;
    }

    public async Task<bool> HandleAsync(
        SuspendUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user == null)
            return false;

        user.IsActive = false;

        await _context.SaveChangesAsync(cancellationToken);

        // Broadcast: all connected admin clients update their user status badge immediately
        await _broadcaster.BroadcastUserStatusChangedAsync(user.Id, isActive: false, cancellationToken);

        return true;
    }
}

using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;

namespace VTOS.Application.Features.Admin.Commands;

public class SuspendUserCommandHandler : ISuspendUserCommandHandler
{
    private readonly IApplicationDbContext _context;

    public SuspendUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
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

        return true;
    }
}

using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;

namespace VTOS.Application.Features.Admin.Commands;

public class UnbanUserCommandHandler : IUnbanUserCommandHandler
{
    private readonly IApplicationDbContext _context;

    public UnbanUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HandleAsync(
        UnbanUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user == null)
            return false;

        user.IsDeleted = false;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;

namespace VTOS.Application.Features.Admin.Commands;

public class ApproveUserCommandHandler : IApproveUserCommandHandler
{
    private readonly IApplicationDbContext _context;

    public ApproveUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HandleAsync(
        ApproveUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user == null)
            return false;

        user.IsActive = true;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

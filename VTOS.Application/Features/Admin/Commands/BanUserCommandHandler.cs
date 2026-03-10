using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;

namespace VTOS.Application.Features.Admin.Commands;

public class BanUserCommandHandler : IBanUserCommandHandler
{
    private readonly IApplicationDbContext _context;

    public BanUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HandleAsync(
        BanUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user == null)
            return false;

        user.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
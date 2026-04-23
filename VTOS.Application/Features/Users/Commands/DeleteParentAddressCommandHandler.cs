using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Users.Commands;

public class DeleteParentAddressCommandHandler : IDeleteParentAddressCommandHandler
{
    private readonly IApplicationDbContext _context;

    public DeleteParentAddressCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> HandleAsync(
        DeleteParentAddressCommand command,
        CancellationToken cancellationToken = default)
    {
        var address = await _context.ParentAddresses
            .FirstOrDefaultAsync(
                item => item.Id == command.AddressId && item.ParentUserID == command.ParentUserId,
                cancellationToken);

        if (address == null)
        {
            return Result.Failure("Address not found.", "PARENT_ADDRESS_NOT_FOUND");
        }

        var wasDefault = address.IsDefault;
        _context.ParentAddresses.Remove(address);
        await _context.SaveChangesAsync(cancellationToken);

        if (wasDefault)
        {
            var fallback = await _context.ParentAddresses
                .Where(item => item.ParentUserID == command.ParentUserId)
                .OrderByDescending(item => item.UpdatedAt ?? item.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (fallback != null)
            {
                fallback.IsDefault = true;
                fallback.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return Result.Success();
    }
}

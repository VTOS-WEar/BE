using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Users.Commands;

public class SetDefaultParentAddressCommandHandler : ISetDefaultParentAddressCommandHandler
{
    private readonly IApplicationDbContext _context;

    public SetDefaultParentAddressCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> HandleAsync(
        SetDefaultParentAddressCommand command,
        CancellationToken cancellationToken = default)
    {
        var addresses = await _context.ParentAddresses
            .Where(item => item.ParentUserID == command.ParentUserId)
            .ToListAsync(cancellationToken);

        var target = addresses.FirstOrDefault(item => item.Id == command.AddressId);
        if (target == null)
        {
            return Result.Failure("Address not found.", "PARENT_ADDRESS_NOT_FOUND");
        }

        var utcNow = DateTime.UtcNow;
        foreach (var address in addresses)
        {
            address.IsDefault = address.Id == command.AddressId;
            address.UpdatedAt = utcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

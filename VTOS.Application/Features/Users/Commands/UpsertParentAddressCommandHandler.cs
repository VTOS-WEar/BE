using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Users.DTOs;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Users.Commands;

public class UpsertParentAddressCommandHandler : IUpsertParentAddressCommandHandler
{
    private readonly IApplicationDbContext _context;

    public UpsertParentAddressCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ParentAddressResponse>> HandleAsync(
        UpsertParentAddressCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Label) ||
            string.IsNullOrWhiteSpace(command.RecipientName) ||
            string.IsNullOrWhiteSpace(command.RecipientPhone) ||
            string.IsNullOrWhiteSpace(command.AddressLine))
        {
            return Result<ParentAddressResponse>.Failure("Address fields are required.", "INVALID_PARENT_ADDRESS");
        }

        var user = await _context.Users
            .AsNoTracking()
            .Include(item => item.Role)
            .FirstOrDefaultAsync(item => item.Id == command.ParentUserId, cancellationToken);

        if (user == null)
        {
            return Result<ParentAddressResponse>.Failure("User not found.", "USER_NOT_FOUND");
        }

        if (user.Role?.RoleName != "Parent")
        {
            return Result<ParentAddressResponse>.Failure("Only parents can manage addresses.", "FORBIDDEN");
        }

        ParentAddress address;
        var utcNow = DateTime.UtcNow;

        if (command.AddressId.HasValue)
        {
            address = await _context.ParentAddresses
                .FirstOrDefaultAsync(
                    item => item.Id == command.AddressId.Value && item.ParentUserID == command.ParentUserId,
                    cancellationToken);

            if (address == null)
            {
                return Result<ParentAddressResponse>.Failure("Address not found.", "PARENT_ADDRESS_NOT_FOUND");
            }

            address.Label = command.Label.Trim();
            address.RecipientName = command.RecipientName.Trim();
            address.RecipientPhone = command.RecipientPhone.Trim();
            address.AddressLine = command.AddressLine.Trim();
            address.UpdatedAt = utcNow;
        }
        else
        {
            address = new ParentAddress
            {
                Id = Guid.NewGuid(),
                ParentUserID = command.ParentUserId,
                Label = command.Label.Trim(),
                RecipientName = command.RecipientName.Trim(),
                RecipientPhone = command.RecipientPhone.Trim(),
                AddressLine = command.AddressLine.Trim(),
                CreatedAt = utcNow,
            };
            _context.ParentAddresses.Add(address);
        }

        if (command.IsDefault)
        {
            var siblings = await _context.ParentAddresses
                .Where(item => item.ParentUserID == command.ParentUserId && item.Id != address.Id && item.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var sibling in siblings)
            {
                sibling.IsDefault = false;
                sibling.UpdatedAt = utcNow;
            }
        }
        else if (!command.AddressId.HasValue)
        {
            var existingDefault = await _context.ParentAddresses
                .AnyAsync(item => item.ParentUserID == command.ParentUserId && item.IsDefault, cancellationToken);

            if (!existingDefault)
            {
                address.IsDefault = true;
            }
        }

        if (command.IsDefault)
        {
            address.IsDefault = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<ParentAddressResponse>.Success(new ParentAddressResponse(
            address.Id,
            address.Label,
            address.RecipientName,
            address.RecipientPhone,
            address.AddressLine,
            address.IsDefault));
    }
}

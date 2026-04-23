using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Users.DTOs;

namespace VTOS.Application.Features.Users.Queries;

public class GetParentAddressesQueryHandler : IGetParentAddressesQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetParentAddressesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<ParentAddressResponse>>> HandleAsync(
        GetParentAddressesQuery query,
        CancellationToken cancellationToken = default)
    {
        var addresses = await _context.ParentAddresses
            .AsNoTracking()
            .Where(address => address.ParentUserID == query.ParentUserId)
            .OrderByDescending(address => address.IsDefault)
            .ThenByDescending(address => address.UpdatedAt ?? address.CreatedAt)
            .Select(address => new ParentAddressResponse(
                address.Id,
                address.Label,
                address.RecipientName,
                address.RecipientPhone,
                address.AddressLine,
                address.IsDefault))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ParentAddressResponse>>.Success(addresses);
    }
}

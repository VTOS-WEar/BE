using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Providers.DTOs;

namespace VTOS.Application.Features.Providers.Commands;

public class UpdateProviderProfileCommandHandler : IUpdateProviderProfileCommandHandler
{
    private readonly IApplicationDbContext _context;

    public UpdateProviderProfileCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProviderProfileDto>> HandleAsync(
        UpdateProviderProfileCommand command, CancellationToken ct = default)
    {
        var user = await _context.Users
            .Include(u => u.Provider)
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user == null)
            return Result<ProviderProfileDto>.Failure("User not found.", "USER_NOT_FOUND");

        if (user.Provider == null)
            return Result<ProviderProfileDto>.Failure("No provider linked to this user.", "NO_PROVIDER");

        var p = user.Provider;

        // Partial update — only overwrite non-null fields
        if (command.ProviderName != null) p.ProviderName = command.ProviderName;
        if (command.ContactPersonName != null) p.ContactPersonName = command.ContactPersonName;
        if (command.Phone != null) p.Phone = command.Phone;
        if (command.Email != null) p.Email = command.Email;
        if (command.Address != null) p.Address = command.Address;

        await _context.SaveChangesAsync(ct);

        return Result<ProviderProfileDto>.Success(new ProviderProfileDto(
            p.Id,
            p.ProviderName,
            p.ContactPersonName,
            p.Phone,
            p.Email,
            p.Address,
            p.Status
        ));
    }
}

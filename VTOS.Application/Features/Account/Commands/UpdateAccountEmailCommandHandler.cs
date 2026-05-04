using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Account.DTOs;

namespace VTOS.Application.Features.Account.Commands;

public class UpdateAccountEmailCommandHandler : IUpdateAccountEmailCommandHandler
{
    private readonly IApplicationDbContext _context;

    public UpdateAccountEmailCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UpdateAccountEmailResponse>> HandleAsync(
        UpdateAccountEmailCommand command,
        CancellationToken ct = default)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == command.UserId && !u.IsDeleted, ct);

        if (user == null)
            return Result<UpdateAccountEmailResponse>.Failure("User not found.", "USER_NOT_FOUND");

        var emailExists = await _context.Users
            .AnyAsync(u =>
                u.Id != user.Id &&
                !u.IsDeleted &&
                u.Email.ToLower() == normalizedEmail,
                ct);

        if (emailExists)
            return Result<UpdateAccountEmailResponse>.Failure(
                "Email already exists.",
                "EMAIL_ALREADY_EXISTS");

        if (!string.Equals(user.Email, normalizedEmail, StringComparison.Ordinal))
        {
            user.Email = normalizedEmail;
            await _context.SaveChangesAsync(ct);
        }

        return Result<UpdateAccountEmailResponse>.Success(new UpdateAccountEmailResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.Role.RoleName,
            user.Phone));
    }
}

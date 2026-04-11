using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Auth.DTOs;

namespace VTOS.Application.Features.Auth.Commands;

/// <summary>
/// Command for verifying and saving a parent's phone number.
/// Child linking is now done separately via POST /api/users/me/find-children.
/// Requires authenticated user.
/// </summary>
public record VerifyPhoneCommand(
    Guid UserId, // From JWT claims
    string Phone
);

/// <summary>
/// Handler for phone verification command.
/// Only saves the phone number to the user's account.
/// </summary>
public class VerifyPhoneCommandHandler
{
    private readonly IApplicationDbContext _context;

    public VerifyPhoneCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<VerifyPhoneResponse>> HandleAsync(
        VerifyPhoneCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get user
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user == null)
        {
            return Result<VerifyPhoneResponse>.Failure(
                "User not found",
                "USER_NOT_FOUND");
        }

        // Normalize phone before checking and storing (handles +84, 84, 0 prefixes)
        var normalizedPhone = NormalizePhone(command.Phone);

        // Check if phone is already used by another user
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Phone == normalizedPhone && u.Id != command.UserId, cancellationToken);

        if (existingUser != null)
        {
            return Result<VerifyPhoneResponse>.Failure(
                "Số điện thoại này đã được sử dụng bởi tài khoản khác.",
                "PHONE_ALREADY_USED");
        }

        // Save normalized phone to user record
        user.Phone = normalizedPhone;
        await _context.SaveChangesAsync(cancellationToken);

        return Result<VerifyPhoneResponse>.Success(new VerifyPhoneResponse(
            normalizedPhone,
            "Số điện thoại đã được lưu thành công."
        ));
    }

    /// <summary>
    /// Normalizes a phone number to local "0xxxxxxxxx" format.
    /// Handles +84, 84, 0 prefixes — all stored as local format.
    /// </summary>
    private static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "";
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("84")) digits = digits[2..];
        if (digits.StartsWith("0")) digits = digits[1..];
        return "0" + digits;
    }
}

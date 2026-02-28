using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Users.DTOs;

namespace VTOS.Application.Features.Users.Commands;

/// <summary>
/// Handler for updating user profile (avatar, name, phone).
/// </summary>
public class SubmitVerificationCommandHandler : ISubmitVerificationCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IImageUploadService _imageUploadService;

    public SubmitVerificationCommandHandler(
        IApplicationDbContext context,
        IImageUploadService imageUploadService)
    {
        _context = context;
        _imageUploadService = imageUploadService;
    }

    public async Task<Result<SubmitVerificationResponse>> HandleAsync(
        SubmitVerificationCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get user
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == command.UserId && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            return Result<SubmitVerificationResponse>.Failure("User not found.", "USER_NOT_FOUND");
        }

        bool isUpdated = false;

        // Update FullName if provided
        if (!string.IsNullOrWhiteSpace(command.FullName) && command.FullName != user.FullName)
        {
            user.FullName = command.FullName;
            isUpdated = true;
        }

        // Update Phone if provided
        if (!string.IsNullOrWhiteSpace(command.Phone) && command.Phone != user.Phone)
        {
            user.Phone = command.Phone;
            isUpdated = true;
        }

        // Update Avatar if provided
        if (command.Avatar != null)
        {
            try
            {
                string avatarUrl;
                await using (var stream = command.Avatar.OpenReadStream())
                {
                    avatarUrl = await _imageUploadService.UploadAsync(
                        stream,
                        command.Avatar.FileName,
                        cancellationToken);
                }
                user.Avatar = avatarUrl;
                isUpdated = true;
            }
            catch (Exception ex)
            {
                return Result<SubmitVerificationResponse>.Failure(
                    $"Failed to upload avatar: {ex.Message}",
                    "AVATAR_UPLOAD_FAILED");
            }
        }

        if (!isUpdated)
        {
            return Result<SubmitVerificationResponse>.Success(new SubmitVerificationResponse(
                user.Id,
                user.Email,
                user.FullName,
                user.Phone ?? string.Empty,
                user.Avatar,
                user.Gender.ToString(),
                user.Role.RoleName,
                user.IsActive,
                user.CreatedAt,
                DateTime.UtcNow,
                "No updates were made."
            ));
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<SubmitVerificationResponse>.Success(new SubmitVerificationResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.Phone ?? string.Empty,
            user.Avatar,
            user.Gender.ToString(),
            user.Role.RoleName,
            user.IsActive,
            user.CreatedAt,
            DateTime.UtcNow,
            "Profile updated successfully."
        ));
    }
}

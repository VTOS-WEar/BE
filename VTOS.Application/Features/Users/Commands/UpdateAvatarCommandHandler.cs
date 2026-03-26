using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Users.DTOs;

namespace VTOS.Application.Features.Users.Commands;

public class UpdateAvatarCommandHandler : IUpdateAvatarCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IImageUploadService _imageUploadService;

    public UpdateAvatarCommandHandler(
        IApplicationDbContext context,
        IImageUploadService imageUploadService)
    {
        _context = context;
        _imageUploadService = imageUploadService;
    }

    public async Task<Result<UpdateAvatarResponse>> HandleAsync(UpdateAvatarCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken);

        if (user == null)
            return Result<UpdateAvatarResponse>.Failure("User not found", "USER_NOT_FOUND");

        // Upload new avatar
        string avatarUrl;
        await using (var stream = command.Avatar.OpenReadStream())
        {
            avatarUrl = await _imageUploadService.UploadAsync(
                stream,
                command.Avatar.FileName,
                "avatars",
                cancellationToken
            );
        }

        // Update user avatar
        user.Avatar = avatarUrl;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<UpdateAvatarResponse>.Success(new UpdateAvatarResponse(
            user.Id,
            avatarUrl
        ));
    }
}
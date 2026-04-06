using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Children.DTOs;

namespace VTOS.Application.Features.Children.Commands;

public class UpdateChildAvatarCommandHandler : IUpdateChildAvatarCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IImageUploadService _imageUploadService;

    public UpdateChildAvatarCommandHandler(
        IApplicationDbContext context,
        IImageUploadService imageUploadService)
    {
        _context = context;
        _imageUploadService = imageUploadService;
    }

    public async Task<Result<UpdateChildProfileResponse>> HandleAsync(UpdateChildAvatarCommand command, CancellationToken cancellationToken = default)
    {
        var child = await _context.ChildProfiles
            .Include(x => x.School)
            .FirstOrDefaultAsync(x => x.Id == command.ChildId, cancellationToken);

        if (child == null)
            return Result<UpdateChildProfileResponse>.Failure("Child not found", "CHILD_NOT_FOUND");

        // Upload new avatar
        string avatarUrl;
        await using (var stream = command.Avatar.OpenReadStream())
        {
            avatarUrl = await _imageUploadService.UploadAsync(
                stream,
                command.Avatar.FileName,
                "avatars/children",
                cancellationToken
            );
        }

        // Update child avatar
        child.Avatar = avatarUrl;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<UpdateChildProfileResponse>.Success(
            new UpdateChildProfileResponse(
                child.Id,
                child.FullName,
                child.Age,
                child.Grade,
                child.Gender.ToString(),
                child.School.SchoolName,
                child.SchoolID,
                child.Avatar,
                new ChildBodyMetricDto(child.HeightCm, child.WeightKg),
                IsPhysicallyPossible(child.HeightCm, child.WeightKg)
            )
        );
    }

    private static bool IsPhysicallyPossible(int heightCm, float weightKg)
    {
        return heightCm >= 50 && heightCm <= 200
            && weightKg >= 5 && weightKg <= 120;
    }
}

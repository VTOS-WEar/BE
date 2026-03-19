using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Update an existing outfit. Only updates fields that are non-null in the command (partial update).
/// Validates that the outfit belongs to the current school.
/// </summary>
public class UpdateOutfitCommandHandler : IUpdateOutfitCommandHandler
{
    private readonly IApplicationDbContext _db;

    public UpdateOutfitCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<OutfitDto>> HandleAsync(UpdateOutfitCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user == null)
            return Result<OutfitDto>.Failure("User not found.", "USER_NOT_FOUND");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        if (schoolMgr == null)
            return Result<OutfitDto>.Failure("School profile not set up yet.", "SCHOOL_NOT_FOUND");

        var outfit = await _db.Outfits
            .FirstOrDefaultAsync(o => o.Id == command.OutfitId && !o.IsDeleted, ct);

        if (outfit == null)
            return Result<OutfitDto>.Failure("Outfit not found.", "OUTFIT_NOT_FOUND");

        // Security: ensure the outfit belongs to this school
        if (outfit.SchoolID != schoolMgr.SchoolID)
            return Result<OutfitDto>.Failure("You do not have permission to update this outfit.", "OUTFIT_NOT_FOUND");

        // Partial update — only apply non-null fields
        if (command.OutfitName != null) outfit.OutfitName = command.OutfitName;
        if (command.Description != null) outfit.Description = command.Description;
        if (command.Price.HasValue) outfit.Price = command.Price.Value;
        if (command.OutfitType.HasValue) outfit.OutfitType = command.OutfitType.Value;
        if (command.MainImageURL != null) outfit.MainImageURL = command.MainImageURL;
        if (command.SizeChartID.HasValue) outfit.SizeChartID = command.SizeChartID.Value;
        if (command.IsAvailable.HasValue) outfit.IsAvailable = command.IsAvailable.Value;
        if (command.IsCustomizable.HasValue) outfit.IsCustomizable = command.IsCustomizable.Value;

        outfit.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Result<OutfitDto>.Success(new OutfitDto
        {
            OutfitId = outfit.Id,
            OutfitName = outfit.OutfitName,
            Description = outfit.Description,
            Price = outfit.Price,
            OutfitType = outfit.OutfitType,
            MainImageURL = outfit.MainImageURL,
            SizeChartID = outfit.SizeChartID,
            IsAvailable = outfit.IsAvailable,
            IsCustomizable = outfit.IsCustomizable,
            CreatedAt = outfit.CreatedAt,
            UpdatedAt = outfit.UpdatedAt
        });
    }
}

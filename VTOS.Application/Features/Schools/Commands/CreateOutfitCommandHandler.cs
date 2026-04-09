using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.Commands;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// UC: Create a new outfit for the logged-in school.
/// Resolves SchoolID from UserId, then creates the Outfit record.
/// </summary>
public class CreateOutfitCommandHandler : ICreateOutfitCommandHandler
{
    private readonly IApplicationDbContext _db;

    public CreateOutfitCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<OutfitDto>> HandleAsync(CreateOutfitCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user == null)
            return Result<OutfitDto>.Failure("User not found.", "USER_NOT_FOUND");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        if (schoolMgr == null)
            return Result<OutfitDto>.Failure("School profile not set up yet. Please create your school profile first.", "SCHOOL_NOT_FOUND");

        // Validate field lengths against DB constraints
        if (string.IsNullOrWhiteSpace(command.OutfitName))
            return Result<OutfitDto>.Failure("Outfit name is required.", "NAME_REQUIRED");
        if (command.OutfitName.Length > 50)
            return Result<OutfitDto>.Failure("Outfit name cannot exceed 50 characters.", "NAME_TOO_LONG");
        if (command.Description != null && command.Description.Length > 500)
            return Result<OutfitDto>.Failure("Description cannot exceed 500 characters.", "DESCRIPTION_TOO_LONG");
        if (command.MainImageURL != null && command.MainImageURL.Length > 500)
            return Result<OutfitDto>.Failure("Image URL cannot exceed 500 characters.", "IMAGE_URL_TOO_LONG");

        var outfit = new Outfit
        {
            Id = Guid.NewGuid(),
            SchoolID = schoolMgr.SchoolID,
            OutfitName = command.OutfitName,
            Description = command.Description,
            Price = command.Price,
            OutfitType = command.OutfitType,
            MainImageURL = command.MainImageURL,
            SizeChartID = command.SizeChartID,
            IsCustomizable = command.IsCustomizable,
            IsAvailable = true, // Available by default when created
            CreatedAt = DateTime.UtcNow
        };

        _db.Outfits.Add(outfit);
        await _db.SaveChangesAsync(ct);

        return Result<OutfitDto>.Success(MapToDto(outfit));
    }

    private static OutfitDto MapToDto(Outfit outfit) => new()
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
    };
}

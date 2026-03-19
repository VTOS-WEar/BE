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

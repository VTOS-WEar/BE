using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Soft-delete an outfit (sets IsDeleted = true).
/// Validates that the outfit belongs to the current school.
/// </summary>
public class DeleteOutfitCommandHandler : IDeleteOutfitCommandHandler
{
    private readonly IApplicationDbContext _db;

    public DeleteOutfitCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<bool>> HandleAsync(DeleteOutfitCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user == null)
            return Result<bool>.Failure("User not found.", "USER_NOT_FOUND");

        if (user.SchoolID == null)
            return Result<bool>.Failure("School profile not set up yet.", "SCHOOL_NOT_FOUND");

        var outfit = await _db.Outfits
            .FirstOrDefaultAsync(o => o.Id == command.OutfitId && !o.IsDeleted, ct);

        if (outfit == null)
            return Result<bool>.Failure("Outfit not found.", "OUTFIT_NOT_FOUND");

        // Security: ensure the outfit belongs to this school
        if (outfit.SchoolID != user.SchoolID.Value)
            return Result<bool>.Failure("You do not have permission to delete this outfit.", "OUTFIT_NOT_FOUND");

        outfit.IsDeleted = true;
        outfit.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}

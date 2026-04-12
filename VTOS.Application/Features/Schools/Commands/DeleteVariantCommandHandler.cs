using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Soft-delete a ProductVariant (sets IsDeleted = true).
/// Validates that the variant's outfit belongs to the current school.
/// </summary>
public class DeleteVariantCommandHandler : IDeleteVariantCommandHandler
{
    private readonly IApplicationDbContext _db;

    public DeleteVariantCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<bool>> HandleAsync(DeleteVariantCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user == null)
            return Result<bool>.Failure("User not found.", "USER_NOT_FOUND");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        if (schoolMgr == null)
            return Result<bool>.Failure("School profile not set up yet.", "SCHOOL_NOT_FOUND");

        // Verify outfit belongs to this school
        var outfit = await _db.Outfits
            .FirstOrDefaultAsync(o => o.Id == command.OutfitId && !o.IsDeleted, ct);

        if (outfit == null)
            return Result<bool>.Failure("Outfit not found.", "OUTFIT_NOT_FOUND");

        if (outfit.SchoolID != schoolMgr.SchoolID)
            return Result<bool>.Failure("You do not have permission to modify this outfit.", "OUTFIT_NOT_FOUND");

        // Find the variant
        var variant = await _db.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == command.VariantId && v.OutfitID == command.OutfitId && !v.IsDeleted, ct);

        if (variant == null)
            return Result<bool>.Failure("Variant not found.", "VARIANT_NOT_FOUND");

        variant.IsDeleted = true;

        if (outfit.SizeChartID != null)
        {
            var detail = await _db.SizeChartDetails
                .Include(d => d.Measurements)
                .FirstOrDefaultAsync(d => d.SizeChartID == outfit.SizeChartID && d.SizeLabel == variant.Size, ct);
            if (detail != null)
            {
                _db.SizeChartDetails.Remove(detail);
            }
        }

        await _db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}

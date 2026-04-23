using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Soft-delete an outfit (sets IsDeleted = true).
/// Validates that the outfit belongs to the current school.
/// Blocked if the outfit is linked to a non-draft semester publication.
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

        var schoolMgr = await _db.SchoolManagers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        if (schoolMgr == null)
            return Result<bool>.Failure("School profile not set up yet.", "SCHOOL_NOT_FOUND");

        var outfit = await _db.Outfits
            .FirstOrDefaultAsync(o => o.Id == command.OutfitId && !o.IsDeleted, ct);

        if (outfit == null)
            return Result<bool>.Failure("Outfit not found.", "OUTFIT_NOT_FOUND");

        if (outfit.SchoolID != schoolMgr.SchoolID)
            return Result<bool>.Failure("You do not have permission to delete this outfit.", "OUTFIT_NOT_FOUND");

        var isInPublication = await _db.SemesterPublicationOutfits
            .AsNoTracking()
            .AnyAsync(spo =>
                spo.OutfitID == outfit.Id &&
                spo.SemesterPublication.Status != SemesterPublicationStatus.Draft, ct);

        if (isInPublication)
            return Result<bool>.Failure(
                "Khong the xoa dong phuc da duoc dua vao dot cong bo hoc ky.",
                "OUTFIT_IN_ACTIVE_PUBLICATION");

        outfit.IsDeleted = true;
        outfit.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}

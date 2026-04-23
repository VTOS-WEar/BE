using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// Returns all non-deleted outfits belonging to the logged-in school.
/// Optional filter: IsAvailable.
/// Includes CanDelete flag: false if outfit is linked to any non-draft semester publication.
/// </summary>
public class GetSchoolOutfitsQueryHandler : IGetSchoolOutfitsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetSchoolOutfitsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<OutfitListResponse>> HandleAsync(GetSchoolOutfitsQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == query.UserId, ct);

        if (user == null)
            return Result<OutfitListResponse>.Failure("User not found.", "USER_NOT_FOUND");

        var schoolMgr = await _db.SchoolManagers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        if (schoolMgr == null)
            return Result<OutfitListResponse>.Failure("School profile not set up yet.", "SCHOOL_NOT_FOUND");

        var outfitsQuery = _db.Outfits
            .AsNoTracking()
            .Where(o => o.SchoolID == schoolMgr.SchoolID && !o.IsDeleted);

        if (query.IsAvailable.HasValue)
            outfitsQuery = outfitsQuery.Where(o => o.IsAvailable == query.IsAvailable.Value);

        var nonDeletableIds = await _db.SemesterPublicationOutfits
            .AsNoTracking()
            .Where(spo =>
                spo.Outfit.SchoolID == schoolMgr.SchoolID &&
                !spo.Outfit.IsDeleted &&
                spo.SemesterPublication.Status != SemesterPublicationStatus.Draft)
            .Select(spo => spo.OutfitID)
            .Distinct()
            .ToListAsync(ct);

        var nonDeletableSet = new HashSet<Guid>(nonDeletableIds);

        var outfits = await outfitsQuery
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OutfitDto
            {
                OutfitId = o.Id,
                OutfitName = o.OutfitName,
                Description = o.Description,
                MaterialType = o.MaterialType,
                Price = o.Price,
                OutfitType = o.OutfitType,
                MainImageURL = o.MainImageURL,
                SizeChartID = o.SizeChartID,
                IsAvailable = o.IsAvailable,
                IsCustomizable = o.IsCustomizable,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            })
            .ToListAsync(ct);

        foreach (var outfit in outfits)
            outfit.CanDelete = !nonDeletableSet.Contains(outfit.OutfitId);

        return Result<OutfitListResponse>.Success(new OutfitListResponse
        {
            Items = outfits,
            Total = outfits.Count
        });
    }
}

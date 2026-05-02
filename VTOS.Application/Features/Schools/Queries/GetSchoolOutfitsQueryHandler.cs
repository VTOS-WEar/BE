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

        var baseOutfitsQuery = _db.Outfits
            .AsNoTracking()
            .Where(o => o.SchoolID == schoolMgr.SchoolID && !o.IsDeleted);

        var summary = new OutfitListSummaryDto
        {
            Total = await baseOutfitsQuery.CountAsync(ct),
            Available = await baseOutfitsQuery.CountAsync(o => o.IsAvailable, ct),
            Unavailable = await baseOutfitsQuery.CountAsync(o => !o.IsAvailable, ct)
        };

        var outfitsQuery = baseOutfitsQuery;

        if (query.IsAvailable.HasValue)
            outfitsQuery = outfitsQuery.Where(o => o.IsAvailable == query.IsAvailable.Value);

        if (query.CategoryId.HasValue)
            outfitsQuery = outfitsQuery.Where(o => o.OutfitCategories.Any(oc => oc.CategoryID == query.CategoryId.Value));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            outfitsQuery = outfitsQuery.Where(o =>
                o.OutfitName.ToLower().Contains(search) ||
                (o.Description != null && o.Description.ToLower().Contains(search)) ||
                (o.MaterialType != null && o.MaterialType.ToLower().Contains(search)) ||
                o.OutfitCategories.Any(oc => oc.Category.CategoryName.ToLower().Contains(search)));
        }

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

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var totalCount = await outfitsQuery.CountAsync(ct);

        var outfits = await outfitsQuery
            .Include(o => o.OutfitCategories)
                .ThenInclude(oc => oc.Category)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OutfitDto
            {
                OutfitId = o.Id,
                OutfitName = o.OutfitName,
                Description = o.Description,
                MaterialType = o.MaterialType,
                Price = o.Price,
                OutfitType = o.OutfitType,
                CategoryId = o.OutfitCategories
                    .OrderBy(oc => oc.Category.CategoryName)
                    .Select(oc => (Guid?)oc.CategoryID)
                    .FirstOrDefault(),
                CategoryName = o.OutfitCategories
                    .OrderBy(oc => oc.Category.CategoryName)
                    .Select(oc => oc.Category.CategoryName)
                    .FirstOrDefault(),
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
            Total = totalCount,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Summary = summary
        });
    }
}

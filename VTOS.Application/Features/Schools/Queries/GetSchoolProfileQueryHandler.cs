using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Queries;

/// <summary>
/// UC-42: Get school profile handler.
/// Finds the School linked to the current user via User.SchoolID.
/// </summary>
public class GetSchoolProfileQueryHandler : IGetSchoolProfileQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetSchoolProfileQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<SchoolProfileDto>> HandleAsync(GetSchoolProfileQuery query, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == query.UserId, ct);

        if (user == null || user.SchoolID == null)
            return Result<SchoolProfileDto>.Failure("User is not linked to any school.", "SCHOOL_NOT_LINKED");

        var school = await _db.Schools
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == user.SchoolID.Value, ct);

        if (school == null)
            return Result<SchoolProfileDto>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        var dto = new SchoolProfileDto
        {
            Id = school.Id,
            SchoolName = school.SchoolName,
            LogoURL = school.LogoURL,
            ContactInfo = school.ContactInfo,
            Level = school.Level,
            CatalogID = school.CatalogID,
            CreatedAt = school.CreatedAt,
            UpdatedAt = school.UpdatedAt
        };

        return Result<SchoolProfileDto>.Success(dto);
    }
}

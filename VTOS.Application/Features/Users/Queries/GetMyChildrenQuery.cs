using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Users.DTOs;

namespace VTOS.Application.Features.Users.Queries;

/// <summary>
/// Query to get all children linked to the current parent.
/// </summary>
public record GetMyChildrenQuery(Guid UserId);

public class GetMyChildrenQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetMyChildrenQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<ChildProfileDto>>> HandleAsync(
        GetMyChildrenQuery query,
        CancellationToken cancellationToken = default)
    {
        var children = await _context.ChildProfiles
            .AsNoTracking()
            .Include(c => c.School)
            .Where(c => c.ParentUserID == query.UserId && !c.IsDeleted)
            .OrderBy(c => c.FullName)
            .Select(c => new ChildProfileDto(
                c.Id,
                c.FullName,
                c.Age,
                c.Grade,
                c.ClassGroupID,
                c.ClassGroup != null ? c.ClassGroup.ClassName : null,
                c.ClassGroup != null ? c.ClassGroup.AcademicYear : null,
                c.Gender.ToString(),
                c.Avatar,
                new ChildSchoolDto(c.School.Id, c.School.SchoolName, c.School.LogoURL),
                c.HeightCm,
                c.WeightKg
            ))
            .ToListAsync(cancellationToken);

        return Result<List<ChildProfileDto>>.Success(children);
    }
}

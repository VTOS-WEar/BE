using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Admin.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Queries;

public class GetAdminSemesterPublicationsQueryHandler : IGetAdminSemesterPublicationsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetAdminSemesterPublicationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AdminSemesterPublicationListDto>> HandleAsync(
        GetAdminSemesterPublicationsQuery query,
        CancellationToken ct = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var publicationsQuery = _context.SemesterPublications
            .AsNoTracking()
            .Include(x => x.School)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<SemesterPublicationStatus>(query.Status, true, out var status))
            {
                return Result<AdminSemesterPublicationListDto>.Failure(
                    "Invalid semester publication status.",
                    "INVALID_STATUS");
            }

            publicationsQuery = publicationsQuery.Where(x => x.Status == status);
        }

        var total = await publicationsQuery.CountAsync(ct);

        var items = await publicationsQuery
            .OrderByDescending(x => x.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminSemesterPublicationOptionDto(
                x.Id,
                x.Semester,
                x.AcademicYear,
                x.SchoolID,
                x.School.SchoolName,
                x.Status.ToString(),
                x.StartDate,
                x.EndDate,
                x.Orders.Count
            ))
            .ToListAsync(ct);

        return Result<AdminSemesterPublicationListDto>.Success(
            new AdminSemesterPublicationListDto(items, total));
    }
}

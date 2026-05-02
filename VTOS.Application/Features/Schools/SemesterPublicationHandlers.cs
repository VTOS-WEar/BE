using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.Commands;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Application.Features.Schools.Queries;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools;

internal static class SemesterPublicationMapping
{
    public static SemesterPublicationDto ToDto(
        SemesterPublication publication,
        int outfitCount,
        int providerCount)
        => new(
            publication.Id,
            publication.SchoolID,
            publication.Semester,
            publication.AcademicYear,
            publication.StartDate,
            publication.EndDate,
            publication.Status.ToString(),
            publication.Description,
            publication.Rules,
            outfitCount,
            providerCount,
            publication.CreatedAt,
            publication.UpdatedAt);
}

internal sealed class SemesterPublicationContext
{
    private readonly IApplicationDbContext _db;

    public SemesterPublicationContext(IApplicationDbContext db)
    {
        _db = db;
    }

    public DbSet<SemesterPublication> Publications => _db.Set<SemesterPublication>();
    public DbSet<SemesterPublicationOutfit> PublicationOutfits => _db.Set<SemesterPublicationOutfit>();
    public DbSet<SemesterPublicationProvider> PublicationProviders => _db.Set<SemesterPublicationProvider>();

    public async Task<Result<Guid>> ResolveSchoolIdAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return Result<Guid>.Failure("User not found.", "USER_NOT_FOUND");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr == null)
            return Result<Guid>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        return Result<Guid>.Success(schoolMgr.SchoolID);
    }

    public async Task<SemesterPublicationDetailDto> BuildDetailDtoAsync(SemesterPublication publication, CancellationToken ct)
    {
        var outfits = await PublicationOutfits
            .AsNoTracking()
            .Where(x => x.SemesterPublicationID == publication.Id)
            .Include(x => x.Outfit)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PublicationOutfitDto(
                x.Id,
                x.OutfitID,
                x.Outfit.OutfitName,
                x.Outfit.MainImageURL,
                x.Outfit.Price,
                x.Outfit.OutfitType.ToString(),
                x.Notes,
                x.CreatedAt))
            .ToListAsync(ct);

        var providers = await PublicationProviders
            .AsNoTracking()
            .Where(x => x.SemesterPublicationID == publication.Id)
            .Include(x => x.Provider)
            .Include(x => x.Contract)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PublicationProviderDto(
                x.Id,
                x.ProviderID,
                x.Provider.ProviderName,
                x.Provider.Email,
                x.ContractID,
                x.Contract != null ? x.Contract.ContractName : null,
                x.Status.ToString(),
                x.CreatedAt,
                x.SuspendedAt,
                x.SuspendReason))
            .ToListAsync(ct);

        return new SemesterPublicationDetailDto(
            publication.Id,
            publication.SchoolID,
            publication.Semester,
            publication.AcademicYear,
            publication.StartDate,
            publication.EndDate,
            publication.Status.ToString(),
            publication.Description,
            publication.Rules,
            outfits,
            providers,
            publication.CreatedAt,
            publication.UpdatedAt);
    }
}

public class CreateSemesterPublicationCommandHandler : ICreateSemesterPublicationCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly SemesterPublicationContext _context;

    public CreateSemesterPublicationCommandHandler(IApplicationDbContext db)
    {
        _db = db;
        _context = new SemesterPublicationContext(db);
    }

    public async Task<Result<SemesterPublicationDto>> HandleAsync(CreateSemesterPublicationCommand command, CancellationToken ct = default)
    {
        var schoolIdResult = await _context.ResolveSchoolIdAsync(command.UserId, ct);
        if (!schoolIdResult.IsSuccess)
            return Result<SemesterPublicationDto>.Failure(schoolIdResult.Error!, schoolIdResult.ErrorCode);

        var semester = command.Semester.Trim();
        var academicYear = command.AcademicYear.Trim();

        if (string.IsNullOrWhiteSpace(semester))
            return Result<SemesterPublicationDto>.Failure("Semester is required.", "SEMESTER_REQUIRED");
        if (string.IsNullOrWhiteSpace(academicYear))
            return Result<SemesterPublicationDto>.Failure("Academic year is required.", "ACADEMIC_YEAR_REQUIRED");
        if (command.EndDate <= command.StartDate)
            return Result<SemesterPublicationDto>.Failure("End date must be after start date.", "INVALID_DATE_RANGE");

        var schoolId = schoolIdResult.Value!;
        var exists = await _context.Publications.AsNoTracking().AnyAsync(
            x => x.SchoolID == schoolId && x.Semester == semester && x.AcademicYear == academicYear, ct);
        if (exists)
            return Result<SemesterPublicationDto>.Failure(
                "A semester publication already exists for this semester and academic year.",
                "DUPLICATE_PUBLICATION");

        var publication = new SemesterPublication
        {
            Id = Guid.NewGuid(),
            SchoolID = schoolId,
            Semester = semester,
            AcademicYear = academicYear,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Description = command.Description?.Trim(),
            Rules = command.Rules?.Trim(),
            Status = SemesterPublicationStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        _context.Publications.Add(publication);
        await _db.SaveChangesAsync(ct);

        return Result<SemesterPublicationDto>.Success(SemesterPublicationMapping.ToDto(publication, 0, 0));
    }
}

public class UpdateSemesterPublicationCommandHandler : IUpdateSemesterPublicationCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly SemesterPublicationContext _context;

    public UpdateSemesterPublicationCommandHandler(IApplicationDbContext db)
    {
        _db = db;
        _context = new SemesterPublicationContext(db);
    }

    public async Task<Result<SemesterPublicationDto>> HandleAsync(UpdateSemesterPublicationCommand command, CancellationToken ct = default)
    {
        var schoolIdResult = await _context.ResolveSchoolIdAsync(command.UserId, ct);
        if (!schoolIdResult.IsSuccess)
            return Result<SemesterPublicationDto>.Failure(schoolIdResult.Error!, schoolIdResult.ErrorCode);

        var publication = await _context.Publications
            .FirstOrDefaultAsync(x => x.Id == command.PublicationId && x.SchoolID == schoolIdResult.Value, ct);
        if (publication == null)
            return Result<SemesterPublicationDto>.Failure("Semester publication not found.", "PUBLICATION_NOT_FOUND");
        if (publication.Status != SemesterPublicationStatus.Draft)
            return Result<SemesterPublicationDto>.Failure("Only Draft publications can be updated.", "INVALID_STATUS");

        var semester = command.Semester?.Trim() ?? publication.Semester;
        var academicYear = command.AcademicYear?.Trim() ?? publication.AcademicYear;
        var startDate = command.StartDate ?? publication.StartDate;
        var endDate = command.EndDate ?? publication.EndDate;

        if (string.IsNullOrWhiteSpace(semester))
            return Result<SemesterPublicationDto>.Failure("Semester is required.", "SEMESTER_REQUIRED");
        if (string.IsNullOrWhiteSpace(academicYear))
            return Result<SemesterPublicationDto>.Failure("Academic year is required.", "ACADEMIC_YEAR_REQUIRED");
        if (endDate <= startDate)
            return Result<SemesterPublicationDto>.Failure("End date must be after start date.", "INVALID_DATE_RANGE");

        var duplicate = await _context.Publications.AsNoTracking().AnyAsync(
            x => x.Id != publication.Id
                && x.SchoolID == publication.SchoolID
                && x.Semester == semester
                && x.AcademicYear == academicYear, ct);
        if (duplicate)
            return Result<SemesterPublicationDto>.Failure(
                "A semester publication already exists for this semester and academic year.",
                "DUPLICATE_PUBLICATION");

        publication.Semester = semester;
        publication.AcademicYear = academicYear;
        publication.StartDate = startDate;
        publication.EndDate = endDate;
        publication.Description = command.Description?.Trim();
        publication.Rules = command.Rules?.Trim();
        publication.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var outfitCount = await _context.PublicationOutfits.CountAsync(x => x.SemesterPublicationID == publication.Id, ct);
        var providerCount = await _context.PublicationProviders.CountAsync(x => x.SemesterPublicationID == publication.Id, ct);

        return Result<SemesterPublicationDto>.Success(SemesterPublicationMapping.ToDto(publication, outfitCount, providerCount));
    }
}

public class DeleteDraftPublicationCommandHandler : IDeleteDraftPublicationCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly SemesterPublicationContext _context;

    public DeleteDraftPublicationCommandHandler(IApplicationDbContext db)
    {
        _db = db;
        _context = new SemesterPublicationContext(db);
    }

    public async Task<Result<string>> HandleAsync(DeleteDraftPublicationCommand command, CancellationToken ct = default)
    {
        var schoolIdResult = await _context.ResolveSchoolIdAsync(command.UserId, ct);
        if (!schoolIdResult.IsSuccess)
            return Result<string>.Failure(schoolIdResult.Error!, schoolIdResult.ErrorCode);

        var publication = await _context.Publications
            .FirstOrDefaultAsync(x => x.Id == command.PublicationId && x.SchoolID == schoolIdResult.Value, ct);
        if (publication == null)
            return Result<string>.Failure("Semester publication not found.", "PUBLICATION_NOT_FOUND");
        if (publication.Status != SemesterPublicationStatus.Draft)
            return Result<string>.Failure("Only Draft publications can be deleted.", "INVALID_STATUS");

        var outfits = await _context.PublicationOutfits.Where(x => x.SemesterPublicationID == publication.Id).ToListAsync(ct);
        var providers = await _context.PublicationProviders.Where(x => x.SemesterPublicationID == publication.Id).ToListAsync(ct);

        _context.PublicationOutfits.RemoveRange(outfits);
        _context.PublicationProviders.RemoveRange(providers);
        _context.Publications.Remove(publication);

        await _db.SaveChangesAsync(ct);
        return Result<string>.Success("Semester publication deleted successfully.");
    }
}

public class PublishSemesterPublicationCommandHandler : IPublishSemesterPublicationCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly SemesterPublicationContext _context;

    public PublishSemesterPublicationCommandHandler(IApplicationDbContext db)
    {
        _db = db;
        _context = new SemesterPublicationContext(db);
    }

    public async Task<Result<SemesterPublicationDto>> HandleAsync(PublishSemesterPublicationCommand command, CancellationToken ct = default)
    {
        var schoolIdResult = await _context.ResolveSchoolIdAsync(command.UserId, ct);
        if (!schoolIdResult.IsSuccess)
            return Result<SemesterPublicationDto>.Failure(schoolIdResult.Error!, schoolIdResult.ErrorCode);

        var publication = await _context.Publications
            .FirstOrDefaultAsync(x => x.Id == command.PublicationId && x.SchoolID == schoolIdResult.Value, ct);
        if (publication == null)
            return Result<SemesterPublicationDto>.Failure("Semester publication not found.", "PUBLICATION_NOT_FOUND");
        if (publication.Status != SemesterPublicationStatus.Draft)
            return Result<SemesterPublicationDto>.Failure("Only Draft publications can be published.", "INVALID_STATUS");

        var outfitCount = await _context.PublicationOutfits.CountAsync(x => x.SemesterPublicationID == publication.Id, ct);
        if (outfitCount <= 0)
            return Result<SemesterPublicationDto>.Failure("At least one outfit is required before publishing.", "OUTFIT_REQUIRED");

        var providerCount = await _context.PublicationProviders.CountAsync(
            x => x.SemesterPublicationID == publication.Id && x.Status == SemPublicationProviderStatus.Active, ct);
        if (providerCount <= 0)
            return Result<SemesterPublicationDto>.Failure("At least one active provider is required before publishing.", "PROVIDER_REQUIRED");

        publication.Status = SemesterPublicationStatus.Active;
        publication.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result<SemesterPublicationDto>.Success(SemesterPublicationMapping.ToDto(publication, outfitCount, providerCount));
    }
}

public class CloseSemesterPublicationCommandHandler : ICloseSemesterPublicationCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly SemesterPublicationContext _context;

    public CloseSemesterPublicationCommandHandler(IApplicationDbContext db)
    {
        _db = db;
        _context = new SemesterPublicationContext(db);
    }

    public async Task<Result<SemesterPublicationDto>> HandleAsync(CloseSemesterPublicationCommand command, CancellationToken ct = default)
    {
        var schoolIdResult = await _context.ResolveSchoolIdAsync(command.UserId, ct);
        if (!schoolIdResult.IsSuccess)
            return Result<SemesterPublicationDto>.Failure(schoolIdResult.Error!, schoolIdResult.ErrorCode);

        var publication = await _context.Publications
            .FirstOrDefaultAsync(x => x.Id == command.PublicationId && x.SchoolID == schoolIdResult.Value, ct);
        if (publication == null)
            return Result<SemesterPublicationDto>.Failure("Semester publication not found.", "PUBLICATION_NOT_FOUND");
        if (publication.Status != SemesterPublicationStatus.Active)
            return Result<SemesterPublicationDto>.Failure("Only Active publications can be closed.", "INVALID_STATUS");

        publication.Status = SemesterPublicationStatus.Closed;
        publication.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var outfitCount = await _context.PublicationOutfits.CountAsync(x => x.SemesterPublicationID == publication.Id, ct);
        var providerCount = await _context.PublicationProviders.CountAsync(x => x.SemesterPublicationID == publication.Id, ct);

        return Result<SemesterPublicationDto>.Success(SemesterPublicationMapping.ToDto(publication, outfitCount, providerCount));
    }
}

public class AddOutfitsToPublicationCommandHandler : IAddOutfitsToPublicationCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly SemesterPublicationContext _context;

    public AddOutfitsToPublicationCommandHandler(IApplicationDbContext db)
    {
        _db = db;
        _context = new SemesterPublicationContext(db);
    }

    public async Task<Result<SemesterPublicationDetailDto>> HandleAsync(AddOutfitsToPublicationCommand command, CancellationToken ct = default)
    {
        var schoolIdResult = await _context.ResolveSchoolIdAsync(command.UserId, ct);
        if (!schoolIdResult.IsSuccess)
            return Result<SemesterPublicationDetailDto>.Failure(schoolIdResult.Error!, schoolIdResult.ErrorCode);

        var publication = await _context.Publications
            .FirstOrDefaultAsync(x => x.Id == command.PublicationId && x.SchoolID == schoolIdResult.Value, ct);
        if (publication == null)
            return Result<SemesterPublicationDetailDto>.Failure("Semester publication not found.", "PUBLICATION_NOT_FOUND");
        if (publication.Status != SemesterPublicationStatus.Draft)
            return Result<SemesterPublicationDetailDto>.Failure("Only Draft publications can be modified.", "INVALID_STATUS");
        if (command.OutfitIds == null || command.OutfitIds.Count == 0)
            return Result<SemesterPublicationDetailDto>.Failure("At least one outfit is required.", "OUTFIT_REQUIRED");

        var distinctOutfitIds = command.OutfitIds.Distinct().ToList();
        var outfits = await _db.Outfits.AsNoTracking()
            .Where(x => distinctOutfitIds.Contains(x.Id) && !x.IsDeleted)
            .ToListAsync(ct);
        if (outfits.Count != distinctOutfitIds.Count)
            return Result<SemesterPublicationDetailDto>.Failure("One or more outfits were not found.", "OUTFIT_NOT_FOUND");
        if (outfits.Any(x => x.SchoolID != schoolIdResult.Value))
            return Result<SemesterPublicationDetailDto>.Failure("One or more outfits do not belong to your school.", "OUTFIT_NOT_OWNED");

        var existingOutfitIds = await _context.PublicationOutfits.AsNoTracking()
            .Where(x => x.SemesterPublicationID == publication.Id)
            .Select(x => x.OutfitID)
            .ToListAsync(ct);

        var duplicates = distinctOutfitIds.Intersect(existingOutfitIds).ToList();
        if (duplicates.Count > 0)
            return Result<SemesterPublicationDetailDto>.Failure("One or more outfits are already in this publication.", "DUPLICATE_OUTFIT");

        var now = DateTime.UtcNow;
        foreach (var outfitId in distinctOutfitIds)
        {
            _context.PublicationOutfits.Add(new SemesterPublicationOutfit
            {
                Id = Guid.NewGuid(),
                SemesterPublicationID = publication.Id,
                OutfitID = outfitId,
                Notes = command.Notes?.Trim(),
                CreatedAt = now
            });
        }

        publication.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);

        return Result<SemesterPublicationDetailDto>.Success(await _context.BuildDetailDtoAsync(publication, ct));
    }
}

public class RemoveOutfitFromPublicationCommandHandler : IRemoveOutfitFromPublicationCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly SemesterPublicationContext _context;

    public RemoveOutfitFromPublicationCommandHandler(IApplicationDbContext db)
    {
        _db = db;
        _context = new SemesterPublicationContext(db);
    }

    public async Task<Result<SemesterPublicationDetailDto>> HandleAsync(RemoveOutfitFromPublicationCommand command, CancellationToken ct = default)
    {
        var schoolIdResult = await _context.ResolveSchoolIdAsync(command.UserId, ct);
        if (!schoolIdResult.IsSuccess)
            return Result<SemesterPublicationDetailDto>.Failure(schoolIdResult.Error!, schoolIdResult.ErrorCode);

        var publication = await _context.Publications
            .FirstOrDefaultAsync(x => x.Id == command.PublicationId && x.SchoolID == schoolIdResult.Value, ct);
        if (publication == null)
            return Result<SemesterPublicationDetailDto>.Failure("Semester publication not found.", "PUBLICATION_NOT_FOUND");
        if (publication.Status != SemesterPublicationStatus.Draft)
            return Result<SemesterPublicationDetailDto>.Failure("Only Draft publications can be modified.", "INVALID_STATUS");

        var publicationOutfit = await _context.PublicationOutfits
            .FirstOrDefaultAsync(x => x.Id == command.PublicationOutfitId && x.SemesterPublicationID == publication.Id, ct);
        if (publicationOutfit == null)
            return Result<SemesterPublicationDetailDto>.Failure("Publication outfit not found.", "PUBLICATION_OUTFIT_NOT_FOUND");

        _context.PublicationOutfits.Remove(publicationOutfit);
        publication.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result<SemesterPublicationDetailDto>.Success(await _context.BuildDetailDtoAsync(publication, ct));
    }
}

public class ApproveProviderCommandHandler : IApproveProviderCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly SemesterPublicationContext _context;

    public ApproveProviderCommandHandler(IApplicationDbContext db)
    {
        _db = db;
        _context = new SemesterPublicationContext(db);
    }

    public async Task<Result<SemesterPublicationDetailDto>> HandleAsync(ApproveProviderCommand command, CancellationToken ct = default)
    {
        var schoolIdResult = await _context.ResolveSchoolIdAsync(command.UserId, ct);
        if (!schoolIdResult.IsSuccess)
            return Result<SemesterPublicationDetailDto>.Failure(schoolIdResult.Error!, schoolIdResult.ErrorCode);

        var publication = await _context.Publications
            .FirstOrDefaultAsync(x => x.Id == command.PublicationId && x.SchoolID == schoolIdResult.Value, ct);
        if (publication == null)
            return Result<SemesterPublicationDetailDto>.Failure("Semester publication not found.", "PUBLICATION_NOT_FOUND");
        if (publication.Status != SemesterPublicationStatus.Draft)
            return Result<SemesterPublicationDetailDto>.Failure("Only Draft publications can be modified.", "INVALID_STATUS");

        var provider = await _db.Providers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.ProviderId && !x.IsDeleted, ct);
        if (provider == null)
            return Result<SemesterPublicationDetailDto>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var duplicate = await _context.PublicationProviders.AsNoTracking().AnyAsync(
            x => x.SemesterPublicationID == publication.Id && x.ProviderID == command.ProviderId, ct);
        if (duplicate)
            return Result<SemesterPublicationDetailDto>.Failure("Provider is already approved for this publication.", "DUPLICATE_PROVIDER");

        var now = DateTime.UtcNow;
        var usableContractStatuses = new[] { "Active", "InUse" };
        IQueryable<Contract> contractQuery = _db.Contracts.AsNoTracking()
            .Where(x => x.SchoolID == publication.SchoolID
                     && x.ProviderID == command.ProviderId
                     && usableContractStatuses.Contains(x.Status)
                     && x.ExpiresAt > now);

        if (command.ContractId.HasValue)
            contractQuery = contractQuery.Where(x => x.Id == command.ContractId.Value);

        var contract = await contractQuery
            .OrderByDescending(x => x.ApprovedAt ?? x.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (contract == null)
            return Result<SemesterPublicationDetailDto>.Failure(
                "An active supplier agreement is required to approve this provider.",
                "CONTRACT_NOT_FOUND");

        _context.PublicationProviders.Add(new SemesterPublicationProvider
        {
            Id = Guid.NewGuid(),
            SemesterPublicationID = publication.Id,
            ProviderID = command.ProviderId,
            ContractID = contract.Id,
            Status = SemPublicationProviderStatus.Active,
            CreatedAt = now
        });

        publication.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);

        return Result<SemesterPublicationDetailDto>.Success(await _context.BuildDetailDtoAsync(publication, ct));
    }
}

public class SuspendProviderCommandHandler : ISuspendProviderCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly SemesterPublicationContext _context;

    public SuspendProviderCommandHandler(IApplicationDbContext db)
    {
        _db = db;
        _context = new SemesterPublicationContext(db);
    }

    public async Task<Result<SemesterPublicationDetailDto>> HandleAsync(SuspendProviderCommand command, CancellationToken ct = default)
    {
        var schoolIdResult = await _context.ResolveSchoolIdAsync(command.UserId, ct);
        if (!schoolIdResult.IsSuccess)
            return Result<SemesterPublicationDetailDto>.Failure(schoolIdResult.Error!, schoolIdResult.ErrorCode);

        var publication = await _context.Publications
            .FirstOrDefaultAsync(x => x.Id == command.PublicationId && x.SchoolID == schoolIdResult.Value, ct);
        if (publication == null)
            return Result<SemesterPublicationDetailDto>.Failure("Semester publication not found.", "PUBLICATION_NOT_FOUND");
        if (publication.Status == SemesterPublicationStatus.Closed)
            return Result<SemesterPublicationDetailDto>.Failure("Closed publications cannot be modified.", "INVALID_STATUS");

        var publicationProvider = await _context.PublicationProviders
            .FirstOrDefaultAsync(x => x.Id == command.PublicationProviderId && x.SemesterPublicationID == publication.Id, ct);
        if (publicationProvider == null)
            return Result<SemesterPublicationDetailDto>.Failure("Publication provider not found.", "PUBLICATION_PROVIDER_NOT_FOUND");

        publicationProvider.Status = SemPublicationProviderStatus.Suspended;
        publicationProvider.SuspendReason = command.Reason?.Trim();
        publicationProvider.SuspendedAt = DateTime.UtcNow;
        publicationProvider.UpdatedAt = DateTime.UtcNow;
        publication.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Result<SemesterPublicationDetailDto>.Success(await _context.BuildDetailDtoAsync(publication, ct));
    }
}

public class GetSemesterPublicationsQueryHandler : IGetSemesterPublicationsQueryHandler
{
    private readonly SemesterPublicationContext _context;

    public GetSemesterPublicationsQueryHandler(IApplicationDbContext db)
    {
        _context = new SemesterPublicationContext(db);
    }

    public async Task<Result<GetSemesterPublicationsResponse>> HandleAsync(GetSemesterPublicationsQuery query, CancellationToken ct = default)
    {
        var schoolIdResult = await _context.ResolveSchoolIdAsync(query.UserId, ct);
        if (!schoolIdResult.IsSuccess)
            return Result<GetSemesterPublicationsResponse>.Failure(schoolIdResult.Error!, schoolIdResult.ErrorCode);

        var publicationsQuery = _context.Publications.AsNoTracking()
            .Where(x => x.SchoolID == schoolIdResult.Value);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<SemesterPublicationStatus>(query.Status, true, out var status))
                return Result<GetSemesterPublicationsResponse>.Failure("Invalid publication status.", "INVALID_STATUS");

            publicationsQuery = publicationsQuery.Where(x => x.Status == status);
        }

        var total = await publicationsQuery.CountAsync(ct);
        var publications = await publicationsQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var publicationIds = publications.Select(x => x.Id).ToList();
        var outfitCounts = await _context.PublicationOutfits.AsNoTracking()
            .Where(x => publicationIds.Contains(x.SemesterPublicationID))
            .GroupBy(x => x.SemesterPublicationID)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var providerCounts = await _context.PublicationProviders.AsNoTracking()
            .Where(x => publicationIds.Contains(x.SemesterPublicationID))
            .GroupBy(x => x.SemesterPublicationID)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var items = publications
            .Select(x => SemesterPublicationMapping.ToDto(
                x,
                outfitCounts.GetValueOrDefault(x.Id, 0),
                providerCounts.GetValueOrDefault(x.Id, 0)))
            .ToList();

        return Result<GetSemesterPublicationsResponse>.Success(
            new GetSemesterPublicationsResponse(items, total, query.Page, query.PageSize));
    }
}

public class GetSemesterPublicationDetailQueryHandler : IGetSemesterPublicationDetailQueryHandler
{
    private readonly SemesterPublicationContext _context;

    public GetSemesterPublicationDetailQueryHandler(IApplicationDbContext db)
    {
        _context = new SemesterPublicationContext(db);
    }

    public async Task<Result<SemesterPublicationDetailDto>> HandleAsync(GetSemesterPublicationDetailQuery query, CancellationToken ct = default)
    {
        var schoolIdResult = await _context.ResolveSchoolIdAsync(query.UserId, ct);
        if (!schoolIdResult.IsSuccess)
            return Result<SemesterPublicationDetailDto>.Failure(schoolIdResult.Error!, schoolIdResult.ErrorCode);

        var publication = await _context.Publications.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.PublicationId && x.SchoolID == schoolIdResult.Value, ct);
        if (publication == null)
            return Result<SemesterPublicationDetailDto>.Failure("Semester publication not found.", "PUBLICATION_NOT_FOUND");

        return Result<SemesterPublicationDetailDto>.Success(await _context.BuildDetailDtoAsync(publication, ct));
    }
}

public class GetContractedOutfitSuggestionsQueryHandler : IGetContractedOutfitSuggestionsQueryHandler
{
    private readonly SemesterPublicationContext _context;
    private readonly IApplicationDbContext _db;

    public GetContractedOutfitSuggestionsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
        _context = new SemesterPublicationContext(db);
    }

    public async Task<Result<IReadOnlyList<ContractedOutfitSuggestionDto>>> HandleAsync(GetContractedOutfitSuggestionsQuery query, CancellationToken ct = default)
    {
        var schoolIdResult = await _context.ResolveSchoolIdAsync(query.UserId, ct);
        if (!schoolIdResult.IsSuccess)
            return Result<IReadOnlyList<ContractedOutfitSuggestionDto>>.Failure(schoolIdResult.Error!, schoolIdResult.ErrorCode);

        var items = await _db.Contracts.AsNoTracking()
            .Where(x => x.SchoolID == schoolIdResult.Value && (x.Status == "Active" || x.Status == "InUse"))
            .Include(x => x.ContractItems)
                .ThenInclude(x => x.Outfit)
            .SelectMany(x => x.ContractItems.Select(ci => new ContractedOutfitSuggestionDto(
                ci.OutfitID,
                ci.Outfit.OutfitName,
                ci.Outfit.MainImageURL,
                ci.Outfit.OutfitType.ToString(),
                x.ContractName,
                x.Id)))
            .ToListAsync(ct);

        return Result<IReadOnlyList<ContractedOutfitSuggestionDto>>.Success(items);
    }
}

public class GetContractedProviderSuggestionsQueryHandler : IGetContractedProviderSuggestionsQueryHandler
{
    private readonly SemesterPublicationContext _context;
    private readonly IApplicationDbContext _db;

    public GetContractedProviderSuggestionsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
        _context = new SemesterPublicationContext(db);
    }

    public async Task<Result<IReadOnlyList<ContractedProviderSuggestionDto>>> HandleAsync(GetContractedProviderSuggestionsQuery query, CancellationToken ct = default)
    {
        var schoolIdResult = await _context.ResolveSchoolIdAsync(query.UserId, ct);
        if (!schoolIdResult.IsSuccess)
            return Result<IReadOnlyList<ContractedProviderSuggestionDto>>.Failure(schoolIdResult.Error!, schoolIdResult.ErrorCode);

        var items = await _db.Contracts.AsNoTracking()
            .Where(x => x.SchoolID == schoolIdResult.Value && (x.Status == "Active" || x.Status == "InUse"))
            .Include(x => x.Provider)
            .OrderByDescending(x => x.ApprovedAt ?? x.CreatedAt)
            .Select(x => new ContractedProviderSuggestionDto(
                x.ProviderID,
                x.Provider.ProviderName,
                x.Provider.Email,
                x.Id,
                x.ContractName,
                x.Status))
            .ToListAsync(ct);

        return Result<IReadOnlyList<ContractedProviderSuggestionDto>>.Success(items);
    }
}

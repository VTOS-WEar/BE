using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// UC-42: Create or update school profile handler.
/// - If User has no SchoolID → creates a new School and links it.
/// - If User already has SchoolID → updates existing School.
/// </summary>
public class UpdateSchoolProfileCommandHandler : IUpdateSchoolProfileCommandHandler
{
    private readonly IApplicationDbContext _db;

    public UpdateSchoolProfileCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<SchoolProfileDto>> HandleAsync(UpdateSchoolProfileCommand command, CancellationToken ct = default)
    {
        // Track user (not AsNoTracking) because we may need to set SchoolID
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user == null)
            return Result<SchoolProfileDto>.Failure("User not found.", "USER_NOT_FOUND");

        School school;

        if (user.SchoolID == null)
        {
            // === FIRST TIME: Create new School ===
            if (string.IsNullOrWhiteSpace(command.SchoolName))
                return Result<SchoolProfileDto>.Failure(
                    "School name is required when creating a new school profile.",
                    "SCHOOL_NAME_REQUIRED");

            school = new School
            {
                Id = Guid.NewGuid(),
                SchoolName = command.SchoolName,
                LogoURL = command.LogoURL,
                ContactInfo = command.ContactInfo,
                CreatedAt = DateTime.UtcNow
            };

            _db.Schools.Add(school);

            // Link user to the new school
            user.SchoolID = school.Id;
        }
        else
        {
            // === SUBSEQUENT: Update existing School ===
            school = (await _db.Schools
                .FirstOrDefaultAsync(s => s.Id == user.SchoolID.Value, ct))!;

            if (school == null)
                return Result<SchoolProfileDto>.Failure("School not found.", "SCHOOL_NOT_FOUND");

            if (command.SchoolName != null)
                school.SchoolName = command.SchoolName;
            if (command.LogoURL != null)
                school.LogoURL = command.LogoURL;
            if (command.ContactInfo != null)
                school.ContactInfo = command.ContactInfo;

            school.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        var dto = new SchoolProfileDto
        {
            Id = school.Id,
            SchoolName = school.SchoolName,
            LogoURL = school.LogoURL,
            ContactInfo = school.ContactInfo,
            CatalogID = school.CatalogID,
            CreatedAt = school.CreatedAt,
            UpdatedAt = school.UpdatedAt
        };

        return Result<SchoolProfileDto>.Success(dto);
    }
}


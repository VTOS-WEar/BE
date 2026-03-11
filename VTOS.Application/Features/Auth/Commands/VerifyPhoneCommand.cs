using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Auth.DTOs;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Auth.Commands;

/// <summary>
/// Command for verifying phone and linking children.
/// Requires authenticated user.
/// </summary>
public record VerifyPhoneCommand(
    Guid UserId, // From JWT claims
    string Phone
);

/// <summary>
/// Handler for phone verification command.
/// Fetches children from StudentDataImport based on ParentPhone.
/// </summary>
public class VerifyPhoneCommandHandler
{
    private readonly IApplicationDbContext _context;

    public VerifyPhoneCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<VerifyPhoneResponse>> HandleAsync(
        VerifyPhoneCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get user
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user == null)
        {
            return Result<VerifyPhoneResponse>.Failure(
                "User not found",
                "USER_NOT_FOUND");
        }

        // Check if phone is already used by another parent
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Phone == command.Phone && u.Id != command.UserId, cancellationToken);

        if (existingUser != null)
        {
            return Result<VerifyPhoneResponse>.Failure(
                "Số điện thoại này đã được sử dụng bởi tài khoản khác.",
                "PHONE_ALREADY_USED");
        }

        // Update user phone
        user.Phone = command.Phone;

        // Find matching students from StudentDataImport
        var studentImports = await _context.Set<StudentDataImport>()
            .Include(s => s.School)
            .Where(s => s.ParentPhone == command.Phone && !s.IsRegistered)
            .ToListAsync(cancellationToken);

        var createdChildren = new List<ChildDto>();

        // Create ChildProfile for each match
        foreach (var studentImport in studentImports)
        {
            var childProfile = new ChildProfile
            {
                Id = Guid.NewGuid(),
                ParentUserID = user.Id,
                FullName = studentImport.FullName,
                Age = studentImport.DateOfBirth.HasValue 
                    ? DateTime.UtcNow.Year - studentImport.DateOfBirth.Value.Year 
                    : 0,
                Grade = studentImport.Class ?? string.Empty,
                Gender = Domain.Enums.Gender.Male, // Default, can be updated later
                SchoolID = studentImport.SchoolID,
                IsDeleted = false
            };

            _context.ChildProfiles.Add(childProfile);

            // Update StudentDataImport
            studentImport.IsRegistered = true;
            studentImport.MatchedChildID = childProfile.Id;

            // Add to response
            createdChildren.Add(new ChildDto(
                childProfile.Id,
                childProfile.FullName,
                childProfile.Age,
                childProfile.Grade,
                childProfile.Gender.ToString(),
                new SchoolDto(
                    studentImport.School.Id,
                    studentImport.School.SchoolName,
                    studentImport.School.LogoURL
                )
            ));
        }

        await _context.SaveChangesAsync(cancellationToken);

        var message = createdChildren.Count > 0
            ? $"Successfully linked {createdChildren.Count} children to your account"
            : "No children found with this phone number";

        return Result<VerifyPhoneResponse>.Success(new VerifyPhoneResponse(
            command.Phone,
            createdChildren.Count,
            createdChildren,
            message
        ));
    }
}

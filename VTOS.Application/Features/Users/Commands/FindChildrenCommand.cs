using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Users.DTOs;

namespace VTOS.Application.Features.Users.Commands;

/// <summary>
/// Command to find and link children to a parent account based on stored phone number.
/// Called from "Tìm trẻ" button in parent profile.
/// </summary>
public record FindChildrenCommand(Guid UserId);

/// <summary>
/// Response from find-children operation.
/// </summary>
public record FindChildrenResponse(
    int LinkedCount,
    List<ChildProfileDto> Linked,
    int ConflictedCount,
    List<ConflictedChildDto> Conflicted,
    string Message
);

/// <summary>
/// A child that is already linked to a different parent.
/// </summary>
public record ConflictedChildDto(
    string FullName,
    string Grade,
    string SchoolName,
    string OtherParentName
);

public class FindChildrenCommandHandler
{
    private readonly IApplicationDbContext _context;

    public FindChildrenCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<FindChildrenResponse>> HandleAsync(
        FindChildrenCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get parent user with phone
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user == null)
            return Result<FindChildrenResponse>.Failure("User not found", "USER_NOT_FOUND");

        if (string.IsNullOrEmpty(user.Phone))
            return Result<FindChildrenResponse>.Failure(
                "Bạn chưa cập nhật số điện thoại. Vui lòng cập nhật số điện thoại trước.",
                "PHONE_NOT_SET");

        // Find ChildProfile records whose ParentPhone matches the parent's phone
        // and are NOT yet linked to any parent account
        var candidates = await _context.ChildProfiles
            .Include(c => c.School)
            .Include(c => c.ParentUser)
            .Where(c => c.ParentPhone == user.Phone && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        if (!candidates.Any())
        {
            return Result<FindChildrenResponse>.Success(new FindChildrenResponse(
                0, [], 0, [],
                "Không tìm thấy học sinh nào liên kết với số điện thoại này."
            ));
        }

        var linked = new List<ChildProfileDto>();
        var conflicted = new List<ConflictedChildDto>();

        foreach (var child in candidates)
        {
            // Case 1: Already linked to THIS parent — skip (already in their list)
            if (child.ParentUserID == command.UserId)
                continue;

            // Case 2: Already linked to a DIFFERENT parent — report conflict
            if (child.ParentUserID != null && child.ParentUserID != command.UserId)
            {
                conflicted.Add(new ConflictedChildDto(
                    child.FullName,
                    child.Grade,
                    child.School?.SchoolName ?? "—",
                    child.ParentUser?.FullName ?? "phụ huynh khác"
                ));
                continue;
            }

            // Case 3: Not yet linked — link this child to the parent
            child.ParentUserID = user.Id;

            linked.Add(new ChildProfileDto(
                child.Id,
                child.FullName,
                child.Age,
                child.Grade,
                child.Gender.ToString(),
                child.School != null
                    ? new ChildSchoolDto(child.School.Id, child.School.SchoolName, child.School.LogoURL)
                    : null,
                child.HeightCm,
                child.WeightKg
            ));
        }

        await _context.SaveChangesAsync(cancellationToken);

        var message = linked.Count > 0
            ? $"Đã liên kết {linked.Count} học sinh vào tài khoản của bạn."
            : "Không tìm thấy học sinh mới nào để liên kết.";

        return Result<FindChildrenResponse>.Success(new FindChildrenResponse(
            linked.Count,
            linked,
            conflicted.Count,
            conflicted,
            message
        ));
    }
}

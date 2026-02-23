using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// UC-42: Update school profile.
/// </summary>
public record UpdateSchoolProfileCommand(
    Guid UserId,
    string? SchoolName,
    string? LogoURL,
    string? ContactInfo
);

public interface IUpdateSchoolProfileCommandHandler
{
    Task<Result<SchoolProfileDto>> HandleAsync(UpdateSchoolProfileCommand command, CancellationToken ct = default);
}

using VTOS.Application.Common;
using VTOS.Application.Features.Auth.Commands;
using VTOS.Application.Features.Auth.DTOs;
using VTOS.Application.Features.Users.DTOs;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Users.Commands;
public record UpdateProfileCommand(
    Guid Id,
    string? FullName,
    DateTime? DOB,
    Gender? Gender,
    string? Phone,
    string? Email
);
public interface IUpdateProfileCommandHandler
{
    Task<Result<UpdateProfileResponse>> HandleAsync(UpdateProfileCommand command, CancellationToken cancellationToken = default);
}

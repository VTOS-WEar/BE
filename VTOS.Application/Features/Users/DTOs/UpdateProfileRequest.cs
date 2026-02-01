using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Users.DTOs
{
    public record UpdateProfileRequest
    (
        string? FullName,
        DateTime? DOB,
        Gender? Gender
    );
}
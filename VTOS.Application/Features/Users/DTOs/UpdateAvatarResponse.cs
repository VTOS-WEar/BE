
namespace VTOS.Application.Features.Users.DTOs
{
    public record UpdateAvatarResponse(
        Guid Id,
        string AvatarUrl
    );
}

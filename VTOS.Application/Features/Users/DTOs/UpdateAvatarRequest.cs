using Microsoft.AspNetCore.Http;
namespace VTOS.Application.Features.Users.Commands
{
    public record UpdateAvatarRequest(IFormFile Avatar);
}

using Microsoft.AspNetCore.Http;
using VTOS.Application.Common;
using VTOS.Application.Features.Children.DTOs;

namespace VTOS.Application.Features.Children.Commands;

public record UpdateChildAvatarCommand(
    Guid ChildId,
    IFormFile Avatar
);

public interface IUpdateChildAvatarCommandHandler
{
    Task<Result<UpdateChildProfileResponse>> HandleAsync(UpdateChildAvatarCommand command, CancellationToken cancellationToken = default);
}

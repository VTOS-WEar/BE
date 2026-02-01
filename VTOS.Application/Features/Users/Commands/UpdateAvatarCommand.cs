using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTOS.Application.Common;
using VTOS.Application.Features.Users.DTOs;
using Microsoft.AspNetCore.Http;

namespace VTOS.Application.Features.Users.Commands;

public record UpdateAvatarCommand(
    Guid UserId,
    IFormFile Avatar
);
public interface IUpdateAvatarCommandHandler
{
    Task<Result<UpdateAvatarResponse>> HandleAsync(UpdateAvatarCommand command, CancellationToken cancellationToken = default);
}


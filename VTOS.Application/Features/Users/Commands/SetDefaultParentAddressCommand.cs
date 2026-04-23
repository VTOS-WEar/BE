using VTOS.Application.Common;

namespace VTOS.Application.Features.Users.Commands;

public record SetDefaultParentAddressCommand(Guid ParentUserId, Guid AddressId);

public interface ISetDefaultParentAddressCommandHandler
{
    Task<Result> HandleAsync(SetDefaultParentAddressCommand command, CancellationToken cancellationToken = default);
}

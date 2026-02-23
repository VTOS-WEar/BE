namespace VTOS.Application.Features.Admin.Commands;

public record SuspendUserCommand(Guid UserId);

public interface ISuspendUserCommandHandler
{
    Task<bool> HandleAsync(
        SuspendUserCommand command,
        CancellationToken cancellationToken);
}

namespace VTOS.Application.Features.Admin.Commands;

public record UnbanUserCommand(Guid UserId);

public interface IUnbanUserCommandHandler
{
    Task<bool> HandleAsync(
        UnbanUserCommand command,
        CancellationToken cancellationToken);
}
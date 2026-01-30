using VTOS.Application.Common;

namespace VTOS.Application.Features.TryOn.Commands.GuestTryOn;

/// <summary>
/// Handler interface for guest virtual try-on command
/// </summary>
public interface IGuestTryOnCommandHandler
{
    Task<Result<GuestTryOnResponse>> HandleAsync(GuestTryOnCommand command, CancellationToken cancellationToken = default);
}

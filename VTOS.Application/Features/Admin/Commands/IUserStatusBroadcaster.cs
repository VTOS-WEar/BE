namespace VTOS.Application.Features.Admin.Commands;

/// <summary>
/// Abstraction for broadcasting user status change events to all connected admin clients.
/// Implemented by SignalR in Infrastructure layer.
/// </summary>
public interface IUserStatusBroadcaster
{
    /// <summary>
    /// Broadcasts a user status change (active/inactive) to all admin clients.
    /// </summary>
    Task BroadcastUserStatusChangedAsync(Guid userId, bool isActive, CancellationToken ct = default);
}

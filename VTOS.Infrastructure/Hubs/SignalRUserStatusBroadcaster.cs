using Microsoft.AspNetCore.SignalR;
using VTOS.Application.Features.Admin.Commands;
using VTOS.Infrastructure.Hubs;

namespace VTOS.Infrastructure.Hubs;

/// <summary>
/// SignalR implementation of IUserStatusBroadcaster.
/// Broadcasts "UserStatusChanged" to ALL connected admin clients via Clients.All.
/// This enables the admin users table to update in real-time across all open admin tabs.
/// </summary>
public class SignalRUserStatusBroadcaster : IUserStatusBroadcaster
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRUserStatusBroadcaster(IHubContext<NotificationHub> hubContext) => _hubContext = hubContext;

    public async Task BroadcastUserStatusChangedAsync(Guid userId, bool isActive, CancellationToken ct = default)
    {
        // Broadcast to ALL connected clients (not a specific user group)
        // Admin pages listen on this event to update user status badges instantly
        await _hubContext.Clients.All.SendAsync("UserStatusChanged", userId.ToString(), isActive, ct);
    }
}

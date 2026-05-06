using Microsoft.AspNetCore.SignalR;
using VTOS.Application.Features.Notifications;

namespace VTOS.Infrastructure.Hubs;

/// <summary>
/// Infrastructure implementation of INotificationBroadcaster using SignalR.
/// Pushes real-time "NewNotification" events to individual users via their personal groups.
/// </summary>
public class SignalRNotificationBroadcaster : INotificationBroadcaster
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationBroadcaster(IHubContext<NotificationHub> hubContext) => _hubContext = hubContext;

    public async Task BroadcastToUserAsync(
        Guid userId,
        string title,
        string message,
        string type,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        string? actionUrl = null,
        CancellationToken ct = default)
    {
        var groupName = $"user_{userId}";
        await _hubContext.Clients.Group(groupName).SendAsync("NewNotification", new
        {
            title,
            message,
            type,
            relatedEntityId,
            relatedEntityType,
            actionUrl,
            createdAt = DateTime.UtcNow
        }, ct);
    }
}

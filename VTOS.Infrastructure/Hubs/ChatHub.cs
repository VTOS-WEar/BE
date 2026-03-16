using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace VTOS.Infrastructure.Hubs;

/// <summary>
/// Generic SignalR Hub for real-time chat across Complaints and Contracts.
/// Clients join/leave channel-specific groups to receive targeted messages.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    /// <summary>
    /// Join a specific chat channel group to receive real-time messages.
    /// Group name format: "complaint_{id}" or "contract_{id}"
    /// </summary>
    public async Task JoinChannel(string channelType, string channelId)
    {
        var groupName = $"{channelType.ToLower()}_{channelId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Leave a specific chat channel group.
    /// </summary>
    public async Task LeaveChannel(string channelType, string channelId)
    {
        var groupName = $"{channelType.ToLower()}_{channelId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}

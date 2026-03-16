using Microsoft.AspNetCore.SignalR;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Chat.Queries;

namespace VTOS.Infrastructure.Hubs;

/// <summary>
/// Infrastructure implementation of IChatBroadcaster using SignalR.
/// </summary>
public class SignalRChatBroadcaster : IChatBroadcaster
{
    private readonly IHubContext<ChatHub> _hubContext;

    public SignalRChatBroadcaster(IHubContext<ChatHub> hubContext) => _hubContext = hubContext;

    public async Task BroadcastMessageAsync(string channelType, Guid channelId, ChatMessageDto message, CancellationToken ct = default)
    {
        var groupName = $"{channelType.ToLower()}_{channelId}";
        await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", message, ct);
    }
}

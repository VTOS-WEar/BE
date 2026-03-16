using VTOS.Application.Features.Chat.Queries;

namespace VTOS.Application.Abstractions;

/// <summary>
/// Abstraction for broadcasting chat messages via SignalR.
/// Defined in Application layer to keep clean architecture boundaries.
/// </summary>
public interface IChatBroadcaster
{
    Task BroadcastMessageAsync(string channelType, Guid channelId, ChatMessageDto message, CancellationToken ct = default);
}

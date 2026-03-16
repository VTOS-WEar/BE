using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Chat.Queries;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Chat.Commands;

public record SendChatMessageCommand(Guid UserId, ChatChannelType ChannelType, Guid ChannelId, string Content);

public record SendChatMessageResponse(Guid MessageId, string SenderName, DateTime SentAt);

public interface ISendChatMessageCommandHandler
{
    Task<Result<SendChatMessageResponse>> HandleAsync(SendChatMessageCommand command, CancellationToken ct = default);
}

public class SendChatMessageCommandHandler : ISendChatMessageCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IChatBroadcaster _broadcaster;

    public SendChatMessageCommandHandler(IApplicationDbContext db, IChatBroadcaster broadcaster)
    {
        _db = db;
        _broadcaster = broadcaster;
    }

    public async Task<Result<SendChatMessageResponse>> HandleAsync(SendChatMessageCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Content))
            return Result<SendChatMessageResponse>.Failure("Message content is required.", "CONTENT_REQUIRED");

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null)
            return Result<SendChatMessageResponse>.Failure("User not found.", "USER_NOT_FOUND");

        var hasAccess = await VerifyAccessAsync(user, command.ChannelType, command.ChannelId, ct);
        if (!hasAccess)
            return Result<SendChatMessageResponse>.Failure("Access denied.", "ACCESS_DENIED");

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChannelType = command.ChannelType,
            ChannelId = command.ChannelId,
            SenderUserId = command.UserId,
            SenderName = user.FullName ?? user.Email ?? "Unknown",
            Content = command.Content.Trim(),
            SentAt = DateTime.UtcNow
        };

        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync(ct);

        // Broadcast via SignalR
        await _broadcaster.BroadcastMessageAsync(
            command.ChannelType.ToString(), command.ChannelId,
            new ChatMessageDto(message.Id, message.SenderUserId, message.SenderName, message.Content, message.SentAt, false),
            ct);

        return Result<SendChatMessageResponse>.Success(
            new SendChatMessageResponse(message.Id, message.SenderName, message.SentAt));
    }

    private async Task<bool> VerifyAccessAsync(User user, ChatChannelType channelType, Guid channelId, CancellationToken ct)
    {
        if (channelType == ChatChannelType.Complaint)
        {
            return await _db.Complaints.AsNoTracking().AnyAsync(c =>
                c.Id == channelId && (c.SchoolID == user.SchoolID || c.ProviderID == user.ProviderID), ct);
        }
        else if (channelType == ChatChannelType.Contract)
        {
            return await _db.Contracts.AsNoTracking().AnyAsync(c =>
                c.Id == channelId && (c.SchoolID == user.SchoolID || c.ProviderID == user.ProviderID), ct);
        }

        return false;
    }
}

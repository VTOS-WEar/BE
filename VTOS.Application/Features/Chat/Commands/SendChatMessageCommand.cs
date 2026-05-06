using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Chat.Queries;
using VTOS.Application.Features.Notifications;
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
    private readonly INotificationService _notificationService;

    public SendChatMessageCommandHandler(IApplicationDbContext db, IChatBroadcaster broadcaster, INotificationService notificationService)
    {
        _db = db;
        _broadcaster = broadcaster;
        _notificationService = notificationService;
    }

    public async Task<Result<SendChatMessageResponse>> HandleAsync(SendChatMessageCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Content))
            return Result<SendChatMessageResponse>.Failure("Message content is required.", "CONTENT_REQUIRED");

        if (command.Content.Length > 2000)
            return Result<SendChatMessageResponse>.Failure("Message content cannot exceed 2000 characters.", "CONTENT_TOO_LONG");

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null)
            return Result<SendChatMessageResponse>.Failure("User not found.", "USER_NOT_FOUND");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        var hasAccess = await VerifyAccessAsync(command.UserId, schoolMgr, providerMgr, command.ChannelType, command.ChannelId, ct);
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
            new ChatMessageDto(message.Id, message.SenderUserId, message.SenderName, message.Content, message.SentAt, false,
                message.MessageType.ToString(), message.ImageUrl, message.ProposalStatus, message.ProposalOutfitName),
            ct);

        // Send in-app notifications to other channel members
        try
        {
            var memberIds = await GetChannelMemberIdsAsync(command.ChannelType, command.ChannelId, ct);
            var preview = message.Content.Length > 80 ? message.Content[..80] + "…" : message.Content;

            foreach (var memberId in memberIds.Where(id => id != command.UserId))
            {
                await _notificationService.CreateAsync(
                    memberId,
                    $"Tin nhắn mới từ {message.SenderName}",
                    preview,
                    "ChatMessage",
                    command.ChannelId,
                    command.ChannelType.ToString(),
                    null,
                    ct);
            }
        }
        catch { /* Don't fail message send if notification fails */ }

        return Result<SendChatMessageResponse>.Success(
            new SendChatMessageResponse(message.Id, message.SenderName, message.SentAt));
    }

    /// <summary>
    /// Resolve all user IDs that are members of a given channel.
    /// Reused by both the handler (in-app) and the background digest job (email).
    /// </summary>
    public async Task<List<Guid>> GetChannelMemberIdsAsync(ChatChannelType channelType, Guid channelId, CancellationToken ct)
    {
        var memberIds = new List<Guid>();

        if (channelType == ChatChannelType.SupportTicket)
        {
            var ticket = await _db.SupportTickets.AsNoTracking()
                .Where(t => t.Id == channelId)
                .Select(t => new { t.SchoolID, t.ProviderID })
                .FirstOrDefaultAsync(ct);
            if (ticket != null)
            {
                if (ticket.SchoolID.HasValue)
                    memberIds.AddRange(await _db.SchoolManagers.AsNoTracking()
                        .Where(m => m.SchoolID == ticket.SchoolID.Value).Select(m => m.UserID).ToListAsync(ct));
                if (ticket.ProviderID.HasValue)
                    memberIds.AddRange(await _db.ProviderManagers.AsNoTracking()
                        .Where(m => m.ProviderID == ticket.ProviderID.Value).Select(m => m.UserID).ToListAsync(ct));
            }
        }
        else if (channelType == ChatChannelType.Contract)
        {
            var contract = await _db.Contracts.AsNoTracking()
                .Where(c => c.Id == channelId)
                .Select(c => new { c.SchoolID, c.ProviderID })
                .FirstOrDefaultAsync(ct);
            if (contract != null)
            {
                memberIds.AddRange(await _db.SchoolManagers.AsNoTracking()
                    .Where(m => m.SchoolID == contract.SchoolID).Select(m => m.UserID).ToListAsync(ct));
                memberIds.AddRange(await _db.ProviderManagers.AsNoTracking()
                    .Where(m => m.ProviderID == contract.ProviderID).Select(m => m.UserID).ToListAsync(ct));
            }
        }
        else if (channelType == ChatChannelType.ClassGroup)
        {
            var teacherId = await _db.ClassGroups.AsNoTracking()
                .Where(cg => cg.Id == channelId)
                .Select(cg => cg.HomeroomTeacherID)
                .FirstOrDefaultAsync(ct);
            if (teacherId != null) memberIds.Add(teacherId.Value);

            var parentIds = await _db.ChildProfiles.AsNoTracking()
                .Where(cp => cp.ClassGroupID == channelId && !cp.IsDeleted && cp.ParentUserID != null)
                .Select(cp => cp.ParentUserID!.Value)
                .Distinct()
                .ToListAsync(ct);
            memberIds.AddRange(parentIds);
        }

        return memberIds.Distinct().ToList();
    }

    public static async Task<string> GetChannelLabelAsync(IApplicationDbContext db, ChatChannelType channelType, Guid channelId, CancellationToken ct)
    {
        if (channelType == ChatChannelType.SupportTicket)
        {
            return await db.SupportTickets.AsNoTracking()
                .Where(t => t.Id == channelId).Select(t => t.Title)
                .FirstOrDefaultAsync(ct) ?? "Ticket";
        }
        if (channelType == ChatChannelType.Contract)
        {
            return await db.Contracts.AsNoTracking()
                .Where(c => c.Id == channelId).Select(c => c.ContractName)
                .FirstOrDefaultAsync(ct) ?? "Hợp đồng";
        }
        if (channelType == ChatChannelType.ClassGroup)
        {
            return await db.ClassGroups.AsNoTracking()
                .Where(cg => cg.Id == channelId).Select(cg => cg.ClassName)
                .FirstOrDefaultAsync(ct) ?? "Lớp";
        }
        return "Chat";
    }

    private async Task<bool> VerifyAccessAsync(
        Guid userId,
        SchoolManager? schoolMgr,
        ProviderManager? providerMgr,
        ChatChannelType channelType, Guid channelId, CancellationToken ct)
    {
        var schoolId = schoolMgr?.SchoolID;
        var providerId = providerMgr?.ProviderID;

        if (channelType == ChatChannelType.SupportTicket)
        {
            return await _db.SupportTickets.AsNoTracking().AnyAsync(c =>
                c.Id == channelId && (c.SchoolID == schoolId || c.ProviderID == providerId), ct);
        }
        else if (channelType == ChatChannelType.Contract)
        {
            return await _db.Contracts.AsNoTracking().AnyAsync(c =>
                c.Id == channelId && (c.SchoolID == schoolId || c.ProviderID == providerId), ct);
        }
        else if (channelType == ChatChannelType.ClassGroup)
        {
            return await _db.ClassGroups.AsNoTracking().AnyAsync(cg =>
                       cg.Id == channelId && cg.HomeroomTeacherID == userId, ct)
                   || await _db.ChildProfiles.AsNoTracking().AnyAsync(cp =>
                       cp.ClassGroupID == channelId && cp.ParentUserID == userId && !cp.IsDeleted, ct);
        }

        return false;
    }
}

using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Chat.Queries;

public record GetChatMessagesQuery(Guid UserId, ChatChannelType ChannelType, Guid ChannelId, int Page = 1, int PageSize = 50);

public record ChatMessageDto(
    Guid MessageId,
    Guid SenderUserId,
    string SenderName,
    string Content,
    DateTime SentAt,
    bool IsMe
);

public record GetChatMessagesResponse(
    IReadOnlyList<ChatMessageDto> Items,
    int Total,
    int Page,
    int PageSize
);

public interface IGetChatMessagesQueryHandler
{
    Task<Result<GetChatMessagesResponse>> HandleAsync(GetChatMessagesQuery query, CancellationToken ct = default);
}

public class GetChatMessagesQueryHandler : IGetChatMessagesQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetChatMessagesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<GetChatMessagesResponse>> HandleAsync(GetChatMessagesQuery query, CancellationToken ct = default)
    {
        // Verify user has access to this channel
        var hasAccess = await VerifyAccessAsync(query.UserId, query.ChannelType, query.ChannelId, ct);
        if (!hasAccess)
            return Result<GetChatMessagesResponse>.Failure("Access denied.", "ACCESS_DENIED");

        var q = _db.ChatMessages.AsNoTracking()
            .Where(m => m.ChannelType == query.ChannelType && m.ChannelId == query.ChannelId)
            .OrderByDescending(m => m.SentAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(m => new ChatMessageDto(
                m.Id, m.SenderUserId, m.SenderName,
                m.Content, m.SentAt, m.SenderUserId == query.UserId
            ))
            .ToListAsync(ct);

        // Reverse to chronological order for display
        items.Reverse();

        return Result<GetChatMessagesResponse>.Success(
            new GetChatMessagesResponse(items, total, query.Page, query.PageSize));
    }

    private async Task<bool> VerifyAccessAsync(Guid userId, ChatChannelType channelType, Guid channelId, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return false;

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

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using VTOS.Application.Abstractions;
using VTOS.Domain.Enums;

namespace VTOS.Infrastructure.Hubs;

/// <summary>
/// Generic SignalR Hub for real-time chat across Complaints and Contracts.
/// Clients join/leave channel-specific groups to receive targeted messages.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IApplicationDbContext _db;

    public ChatHub(IApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Join a specific chat channel group to receive real-time messages.
    /// Group name format: "supportticket_{id}", "contract_{id}", or "classgroup_{id}".
    /// </summary>
    public async Task JoinChannel(string channelType, string channelId)
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)
            || !Guid.TryParse(channelId, out var parsedChannelId)
            || !Enum.TryParse<ChatChannelType>(channelType, true, out var parsedChannelType)
            || !await HasAccessAsync(userId, parsedChannelType, parsedChannelId))
        {
            throw new HubException("Access denied.");
        }

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

    private async Task<bool> HasAccessAsync(Guid userId, ChatChannelType channelType, Guid channelId)
    {
        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == userId);
        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == userId);

        var schoolId = schoolMgr?.SchoolID;
        var providerId = providerMgr?.ProviderID;

        if (channelType == ChatChannelType.SupportTicket)
        {
            return await _db.SupportTickets.AsNoTracking().AnyAsync(c =>
                c.Id == channelId && (c.SchoolID == schoolId || c.ProviderID == providerId));
        }

        if (channelType == ChatChannelType.Contract)
        {
            return await _db.Contracts.AsNoTracking().AnyAsync(c =>
                c.Id == channelId && (c.SchoolID == schoolId || c.ProviderID == providerId));
        }

        if (channelType == ChatChannelType.ClassGroup)
        {
            return await _db.ClassGroups.AsNoTracking().AnyAsync(cg =>
                       cg.Id == channelId && cg.HomeroomTeacherID == userId)
                   || await _db.ChildProfiles.AsNoTracking().AnyAsync(cp =>
                       cp.ClassGroupID == channelId && cp.ParentUserID == userId && !cp.IsDeleted);
        }

        return false;
    }
}

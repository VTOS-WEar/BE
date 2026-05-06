using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Notifications;

/// <summary>
/// Service to create in-app notifications for users.
/// Injected into existing handlers to trigger notifications on business actions.
/// </summary>
public interface INotificationService
{
    Task CreateAsync(Guid userId, string title, string message, string type,
        Guid? relatedEntityId = null, string? relatedEntityType = null, string? actionUrl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notify all Admin users. Creates in-app notifications for each admin.
    /// Also pushes real-time via SignalR if available.
    /// Email digest is handled separately by AdminNotificationDigestJob.
    /// </summary>
    Task NotifyAdminsAsync(string title, string message, string type,
        Guid? relatedEntityId = null, string? relatedEntityType = null, string? actionUrl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notify all managers of a School. Resolves SchoolManagers → UserIDs → creates notification + SignalR push.
    /// </summary>
    Task NotifySchoolAsync(Guid schoolId, string title, string message, string type,
        Guid? relatedEntityId = null, string? relatedEntityType = null, string? actionUrl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notify all managers of a Provider. Resolves ProviderManagers → UserIDs → creates notification + SignalR push.
    /// </summary>
    Task NotifyProviderAsync(Guid providerId, string title, string message, string type,
        Guid? relatedEntityId = null, string? relatedEntityType = null, string? actionUrl = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstraction for pushing real-time notification events to connected clients.
/// Implemented by SignalR in Infrastructure layer.
/// </summary>
public interface INotificationBroadcaster
{
    Task BroadcastToUserAsync(
        Guid userId,
        string title,
        string message,
        string type,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        string? actionUrl = null,
        CancellationToken ct = default);
}

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationBroadcaster? _broadcaster;

    public NotificationService(IApplicationDbContext context, INotificationBroadcaster? broadcaster = null)
    {
        _context = context;
        _broadcaster = broadcaster;
    }

    public async Task CreateAsync(Guid userId, string title, string message, string type,
        Guid? relatedEntityId = null, string? relatedEntityType = null, string? actionUrl = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new InAppNotification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            ActionUrl = actionUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.InAppNotifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        // Push real-time
        if (_broadcaster != null)
        {
            try { await _broadcaster.BroadcastToUserAsync(userId, title, message, type, relatedEntityId, relatedEntityType, actionUrl, cancellationToken); }
            catch { /* don't fail */ }
        }
    }

    public async Task NotifyAdminsAsync(string title, string message, string type,
        Guid? relatedEntityId = null, string? relatedEntityType = null, string? actionUrl = null,
        CancellationToken cancellationToken = default)
    {
        // Find all Admin users
        var adminUserIds = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.Role != null && u.Role.RoleName == "Admin" && u.IsActive && !u.IsDeleted)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (adminUserIds.Count == 0) return;

        var now = DateTime.UtcNow;
        var notifications = adminUserIds.Select(adminId => new InAppNotification
        {
            Id = Guid.NewGuid(),
            UserId = adminId,
            Title = title,
            Message = message,
            Type = type,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            ActionUrl = actionUrl,
            IsRead = false,
            CreatedAt = now
        }).ToList();

        _context.InAppNotifications.AddRange(notifications);
        await _context.SaveChangesAsync(cancellationToken);

        // Push real-time to all admins
        if (_broadcaster != null)
        {
            foreach (var adminId in adminUserIds)
            {
                try { await _broadcaster.BroadcastToUserAsync(adminId, title, message, type, relatedEntityId, relatedEntityType, actionUrl, cancellationToken); }
                catch { /* don't fail */ }
            }
        }
    }

    public async Task NotifySchoolAsync(Guid schoolId, string title, string message, string type,
        Guid? relatedEntityId = null, string? relatedEntityType = null, string? actionUrl = null,
        CancellationToken cancellationToken = default)
    {
        var managerUserIds = await _context.SchoolManagers
            .AsNoTracking()
            .Where(m => m.SchoolID == schoolId)
            .Select(m => m.UserID)
            .ToListAsync(cancellationToken);

        foreach (var userId in managerUserIds)
            await CreateAsync(userId, title, message, type, relatedEntityId, relatedEntityType, actionUrl, cancellationToken);
    }

    public async Task NotifyProviderAsync(Guid providerId, string title, string message, string type,
        Guid? relatedEntityId = null, string? relatedEntityType = null, string? actionUrl = null,
        CancellationToken cancellationToken = default)
    {
        var managerUserIds = await _context.ProviderManagers
            .AsNoTracking()
            .Where(m => m.ProviderID == providerId)
            .Select(m => m.UserID)
            .ToListAsync(cancellationToken);

        foreach (var userId in managerUserIds)
            await CreateAsync(userId, title, message, type, relatedEntityId, relatedEntityType, actionUrl, cancellationToken);
    }
}

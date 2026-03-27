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
}

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;

    public NotificationService(IApplicationDbContext context)
    {
        _context = context;
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
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;

namespace VTOS.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that runs every 10 minutes.
/// Finds Admin users who are offline (LastLogin > 10 min ago) and have unread notifications.
/// Sends a batched digest email with all unread notifications, then marks them as "emailed"
/// by recording in NotificationLog to avoid duplicate emails.
/// </summary>
public class AdminNotificationDigestJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AdminNotificationDigestJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan OfflineThreshold = TimeSpan.FromMinutes(10);

    public AdminNotificationDigestJob(
        IServiceScopeFactory scopeFactory,
        ILogger<AdminNotificationDigestJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AdminNotificationDigestJob started.");

        // Initial delay to let the app fully start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDigestsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminNotificationDigestJob.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessDigestsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTime.UtcNow;
        var offlineCutoff = now - OfflineThreshold;

        // Find Admin users who are offline (LastLogin is null or older than threshold)
        var offlineAdmins = await context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.Role != null && u.Role.RoleName == "Admin"
                     && u.IsActive && !u.IsDeleted
                     && (u.LastLogin == null || u.LastLogin < offlineCutoff))
            .Select(u => new { u.Id, u.Email, u.FullName })
            .ToListAsync(ct);

        if (offlineAdmins.Count == 0) return;

        // For each offline admin, find unread notifications that haven't been emailed yet
        foreach (var admin in offlineAdmins)
        {
            // Get IDs of notifications already emailed (tracked via NotificationLog)
            var alreadyEmailedIds = await context.NotificationLogs
                .Where(nl => nl.UserId == admin.Id
                          && nl.NotificationType == Domain.Enums.NotificationType.AdminDigest)
                .Select(nl => nl.ReferenceId)
                .ToListAsync(ct);
            var emailedSet = new HashSet<Guid>(alreadyEmailedIds);

            // Get unread notifications not yet emailed
            var unreadNotifications = await context.InAppNotifications
                .Where(n => n.UserId == admin.Id && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20) // Cap at 20 per digest
                .ToListAsync(ct);

            var newNotifications = unreadNotifications
                .Where(n => !emailedSet.Contains(n.Id))
                .ToList();

            if (newNotifications.Count == 0) continue;

            // Build digest data
            var digestItems = newNotifications
                .Select(n => (n.Title, n.Message, n.CreatedAt))
                .ToList();

            try
            {
                await emailService.SendAdminDigestEmailAsync(
                    admin.Email, admin.FullName, digestItems, ct);

                // Log each notification as emailed to prevent duplicates
                foreach (var n in newNotifications)
                {
                    context.NotificationLogs.Add(new Domain.Entities.NotificationLog
                    {
                        UserId = admin.Id,
                        NotificationType = Domain.Enums.NotificationType.AdminDigest,
                        ReferenceId = n.Id,
                        Email = admin.Email,
                        SentAt = DateTime.UtcNow,
                        IsSuccess = true
                    });
                }

                await context.SaveChangesAsync(ct);
                _logger.LogInformation(
                    "AdminNotificationDigestJob: sent digest to {Email} with {Count} notifications.",
                    admin.Email, newNotifications.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send admin digest to {Email}.", admin.Email);
            }
        }
    }
}

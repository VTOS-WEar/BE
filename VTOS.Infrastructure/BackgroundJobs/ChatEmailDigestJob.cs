using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Chat.Commands;
using VTOS.Domain.Enums;

namespace VTOS.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that runs every 5 minutes.
/// Finds chat messages sent since the last run, groups by channel,
/// resolves channel members, and sends a batched digest email
/// to each member who has new messages they didn't send.
/// Uses NotificationLog with ChatDigest type to avoid duplicate emails.
/// </summary>
public class ChatEmailDigestJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChatEmailDigestJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    public ChatEmailDigestJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ChatEmailDigestJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ChatEmailDigestJob started.");

        // Initial delay to let the app fully start
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDigestsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChatEmailDigestJob.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessDigestsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var notificationLogs = context.Set<Domain.Entities.NotificationLog>();

        var now = DateTime.UtcNow;
        var lookbackWindow = now - Interval - TimeSpan.FromMinutes(1); // slight overlap to avoid gaps

        // Get IDs of chat messages already emailed
        var alreadyEmailedMessageIds = await notificationLogs
            .Where(nl => nl.NotificationType == NotificationType.ChatDigest
                      && nl.SentAt > lookbackWindow)
            .Select(nl => nl.ReferenceId)
            .ToListAsync(ct);
        var emailedSet = new HashSet<Guid>(alreadyEmailedMessageIds);

        // Find recent chat messages not yet digested
        var recentMessages = await context.ChatMessages
            .AsNoTracking()
            .Where(m => m.SentAt > lookbackWindow)
            .OrderBy(m => m.SentAt)
            .ToListAsync(ct);

        var newMessages = recentMessages.Where(m => !emailedSet.Contains(m.Id)).ToList();
        if (newMessages.Count == 0) return;

        // Group by channel
        var channelGroups = newMessages
            .GroupBy(m => new { m.ChannelType, m.ChannelId })
            .ToList();

        // We need a handler instance to resolve members. Create a temporary one with just db access.
        var chatHandler = new SendChatMessageCommandHandler(
            context,
            scope.ServiceProvider.GetRequiredService<IChatBroadcaster>(),
            scope.ServiceProvider.GetRequiredService<Application.Features.Notifications.INotificationService>());

        foreach (var group in channelGroups)
        {
            try
            {
                var channelType = group.Key.ChannelType;
                var channelId = group.Key.ChannelId;
                var messages = group.ToList();

                var channelLabel = await SendChatMessageCommandHandler.GetChannelLabelAsync(context, channelType, channelId, ct);
                var memberIds = await chatHandler.GetChannelMemberIdsAsync(channelType, channelId, ct);

                // Get member info
                var memberInfos = await context.Users.AsNoTracking()
                    .Where(u => memberIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.Email, u.FullName })
                    .ToListAsync(ct);

                // For each member, send digest of messages they didn't send
                foreach (var member in memberInfos)
                {
                    var otherMessages = messages
                        .Where(m => m.SenderUserId != member.Id)
                        .Select(m => (m.SenderName, m.Content, m.SentAt))
                        .ToList();

                    if (otherMessages.Count == 0 || string.IsNullOrWhiteSpace(member.Email))
                        continue;

                    try
                    {
                        await emailService.SendChatDigestEmailAsync(
                            member.Email,
                            member.FullName ?? "Người dùng",
                            channelLabel,
                            channelType.ToString(),
                            otherMessages,
                            ct);

                        _logger.LogInformation(
                            "ChatEmailDigestJob: sent {Count} messages to {Email} for {ChannelType}/{ChannelId}.",
                            otherMessages.Count, member.Email, channelType, channelId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send chat digest to {Email}.", member.Email);
                    }
                }

                // Mark all messages in this group as emailed
                foreach (var msg in messages)
                {
                    notificationLogs.Add(new Domain.Entities.NotificationLog
                    {
                        UserId = Guid.Empty, // channel-level tracking
                        NotificationType = NotificationType.ChatDigest,
                        ReferenceId = msg.Id,
                        Email = "digest",
                        SentAt = DateTime.UtcNow,
                        IsSuccess = true
                    });
                }

                await context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing chat digest for channel {ChannelType}/{ChannelId}.",
                    group.Key.ChannelType, group.Key.ChannelId);
            }
        }
    }
}

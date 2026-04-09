using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Chat.Queries;
using VTOS.Application.Features.Notifications;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Chat.Commands;

// ─── Accept Uniform Proposal (School only) ───

public record AcceptUniformProposalCommand(Guid UserId, Guid MessageId);

public record AcceptUniformProposalResponse(
    Guid OutfitId,
    string OutfitName,
    string? MainImageURL,
    Guid SchoolId
);

public interface IAcceptUniformProposalCommandHandler
{
    Task<Result<AcceptUniformProposalResponse>> HandleAsync(AcceptUniformProposalCommand command, CancellationToken ct = default);
}

public class AcceptUniformProposalCommandHandler : IAcceptUniformProposalCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IChatBroadcaster _broadcaster;
    private readonly INotificationService _notificationService;

    public AcceptUniformProposalCommandHandler(
        IApplicationDbContext db,
        IChatBroadcaster broadcaster,
        INotificationService notificationService)
    {
        _db = db;
        _broadcaster = broadcaster;
        _notificationService = notificationService;
    }

    public async Task<Result<AcceptUniformProposalResponse>> HandleAsync(
        AcceptUniformProposalCommand command, CancellationToken ct = default)
    {
        // 1. Verify user is School
        var schoolMgr = await _db.SchoolManagers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserID == command.UserId, ct);
        if (schoolMgr == null)
            return Result<AcceptUniformProposalResponse>.Failure("Only schools can accept proposals.", "NOT_SCHOOL");

        var schoolId = schoolMgr.SchoolID;

        // 2. Find the proposal message
        var message = await _db.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == command.MessageId
                && m.MessageType == ChatMessageType.UniformProposal
                && m.ProposalStatus == "Pending", ct);

        if (message == null)
            return Result<AcceptUniformProposalResponse>.Failure(
                "Proposal not found or already processed.", "PROPOSAL_NOT_FOUND");

        // 3. Verify School has access to this contract channel
        if (message.ChannelType != ChatChannelType.Contract)
            return Result<AcceptUniformProposalResponse>.Failure("Invalid channel type.", "INVALID_CHANNEL");

        var contract = await _db.Contracts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == message.ChannelId && c.SchoolID == schoolId, ct);
        if (contract == null)
            return Result<AcceptUniformProposalResponse>.Failure("Access denied.", "ACCESS_DENIED");

        // 4. Create new Outfit in School catalog
        var outfitName = message.ProposalOutfitName ?? "Đề xuất từ NCC";
        var outfit = new Outfit
        {
            Id = Guid.NewGuid(),
            SchoolID = schoolId,
            OutfitName = outfitName,
            MainImageURL = message.ImageUrl,
            IsAvailable = true,
            IsDeleted = false,
            Price = 0 // School will set the price later
        };

        _db.Outfits.Add(outfit);

        // 5. Mark proposal as Accepted
        message.ProposalStatus = "Accepted";

        // 6. Auto-send system message WITH image back to chat
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        var systemMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChannelType = message.ChannelType,
            ChannelId = message.ChannelId,
            SenderUserId = command.UserId,
            SenderName = "Hệ thống",
            Content = $"✅ Trường đã chấp nhận đề xuất đồng phục '{outfitName}'",
            SentAt = DateTime.UtcNow,
            MessageType = ChatMessageType.SystemNotification,
            ImageUrl = message.ImageUrl // Same image → Provider sees which one was picked
        };

        _db.ChatMessages.Add(systemMsg);
        await _db.SaveChangesAsync(ct);

        // 7. Broadcast system message via SignalR (real-time)
        await _broadcaster.BroadcastMessageAsync(
            message.ChannelType.ToString(), message.ChannelId,
            new ChatMessageDto(systemMsg.Id, systemMsg.SenderUserId, systemMsg.SenderName,
                systemMsg.Content, systemMsg.SentAt, false,
                systemMsg.MessageType.ToString(), systemMsg.ImageUrl, null, null),
            ct);

        // 8. Notify Provider via bell icon
        try
        {
            await _notificationService.NotifyProviderAsync(contract.ProviderID,
                "✅ Đề xuất đồng phục được chấp nhận",
                $"Trường đã chấp nhận đề xuất đồng phục '{outfitName}'.",
                "Contract", contract.Id, "Contract",
                "/provider/contracts", ct);
        }
        catch { /* Don't fail the main operation */ }

        return Result<AcceptUniformProposalResponse>.Success(
            new AcceptUniformProposalResponse(outfit.Id, outfitName, message.ImageUrl, schoolId));
    }
}

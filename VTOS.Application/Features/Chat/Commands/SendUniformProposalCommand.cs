using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Chat.Queries;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Chat.Commands;

// ─── Send Uniform Proposal (Provider only) ───

public record SendUniformProposalCommand(
    Guid UserId,
    ChatChannelType ChannelType,
    Guid ChannelId,
    string ImageUrl,
    string OutfitName
);

public record SendUniformProposalResponse(Guid MessageId, DateTime SentAt);

public interface ISendUniformProposalCommandHandler
{
    Task<Result<SendUniformProposalResponse>> HandleAsync(SendUniformProposalCommand command, CancellationToken ct = default);
}

public class SendUniformProposalCommandHandler : ISendUniformProposalCommandHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IChatBroadcaster _broadcaster;

    public SendUniformProposalCommandHandler(IApplicationDbContext db, IChatBroadcaster broadcaster)
    {
        _db = db;
        _broadcaster = broadcaster;
    }

    public async Task<Result<SendUniformProposalResponse>> HandleAsync(SendUniformProposalCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.ImageUrl))
            return Result<SendUniformProposalResponse>.Failure("Image URL is required for a uniform proposal.", "IMAGE_REQUIRED");

        if (string.IsNullOrWhiteSpace(command.OutfitName))
            return Result<SendUniformProposalResponse>.Failure("Outfit name is required.", "NAME_REQUIRED");

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null)
            return Result<SendUniformProposalResponse>.Failure("User not found.", "USER_NOT_FOUND");

        // Verify sender is a Provider
        var providerMgr = await _db.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (providerMgr == null)
            return Result<SendUniformProposalResponse>.Failure("Only providers can send uniform proposals.", "NOT_PROVIDER");

        // Verify access to the contract channel
        if (command.ChannelType != ChatChannelType.Contract)
            return Result<SendUniformProposalResponse>.Failure("Uniform proposals can only be sent in contract channels.", "INVALID_CHANNEL");

        var hasAccess = await _db.Contracts.AsNoTracking().AnyAsync(c =>
            c.Id == command.ChannelId && c.ProviderID == providerMgr.ProviderID, ct);
        if (!hasAccess)
            return Result<SendUniformProposalResponse>.Failure("Access denied.", "ACCESS_DENIED");

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChannelType = command.ChannelType,
            ChannelId = command.ChannelId,
            SenderUserId = command.UserId,
            SenderName = user.FullName ?? user.Email ?? "Unknown",
            Content = $"📋 Đề xuất đồng phục: {command.OutfitName}",
            SentAt = DateTime.UtcNow,
            MessageType = ChatMessageType.UniformProposal,
            ImageUrl = command.ImageUrl,
            ProposalStatus = "Pending",
            ProposalOutfitName = command.OutfitName
        };

        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync(ct);

        // Broadcast via SignalR
        await _broadcaster.BroadcastMessageAsync(
            command.ChannelType.ToString(), command.ChannelId,
            new ChatMessageDto(message.Id, message.SenderUserId, message.SenderName,
                message.Content, message.SentAt, false,
                message.MessageType.ToString(), message.ImageUrl,
                message.ProposalStatus, message.ProposalOutfitName),
            ct);

        return Result<SendUniformProposalResponse>.Success(
            new SendUniformProposalResponse(message.Id, message.SentAt));
    }
}

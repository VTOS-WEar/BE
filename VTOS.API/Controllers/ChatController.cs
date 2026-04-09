using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Features.Chat.Commands;
using VTOS.Application.Features.Chat.Queries;
using VTOS.Application.Abstractions;
using VTOS.Domain.Enums;

namespace VTOS.API.Controllers;

/// <summary>
/// Generic chat endpoints for Complaints and Contracts.
/// Both School and Provider roles can use these endpoints.
/// </summary>
[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IGetChatMessagesQueryHandler _getMessagesHandler;
    private readonly ISendChatMessageCommandHandler _sendMessageHandler;
    private readonly ISendUniformProposalCommandHandler _sendProposalHandler;
    private readonly IAcceptUniformProposalCommandHandler _acceptProposalHandler;
    private readonly ICurrentUserService _currentUser;
    private readonly IImageUploadService _imageUploadService;

    public ChatController(
        IGetChatMessagesQueryHandler getMessagesHandler,
        ISendChatMessageCommandHandler sendMessageHandler,
        ISendUniformProposalCommandHandler sendProposalHandler,
        IAcceptUniformProposalCommandHandler acceptProposalHandler,
        ICurrentUserService currentUser,
        IImageUploadService imageUploadService)
    {
        _getMessagesHandler = getMessagesHandler;
        _sendMessageHandler = sendMessageHandler;
        _sendProposalHandler = sendProposalHandler;
        _acceptProposalHandler = acceptProposalHandler;
        _currentUser = currentUser;
        _imageUploadService = imageUploadService;
    }

    /// <summary>Get chat messages for a channel (complaint or contract).</summary>
    [HttpGet("{channelType}/{channelId:guid}/messages")]
    [ProducesResponseType(typeof(GetChatMessagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMessages(
        string channelType,
        Guid channelId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<ChatChannelType>(channelType, true, out var channelEnum))
            return BadRequest(new { error = "Invalid channel type. Use 'complaint' or 'contract'.", code = "INVALID_CHANNEL_TYPE" });

        var result = await _getMessagesHandler.HandleAsync(
            new GetChatMessagesQuery(_currentUser.UserId, channelEnum, channelId, page, pageSize), ct);

        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Send a chat message to a channel (real-time broadcast via SignalR).</summary>
    [HttpPost("{channelType}/{channelId:guid}/messages")]
    [ProducesResponseType(typeof(SendChatMessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMessage(
        string channelType,
        Guid channelId,
        [FromBody] SendMessageRequest request,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<ChatChannelType>(channelType, true, out var channelEnum))
            return BadRequest(new { error = "Invalid channel type. Use 'complaint' or 'contract'.", code = "INVALID_CHANNEL_TYPE" });

        var result = await _sendMessageHandler.HandleAsync(
            new SendChatMessageCommand(_currentUser.UserId, channelEnum, channelId, request.Content), ct);

        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>Provider sends a uniform proposal with image in contract chat.</summary>
    [HttpPost("messages/proposal")]
    [Authorize(Roles = "Provider")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(SendUniformProposalResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendUniformProposal(
        [FromForm] SendUniformProposalRequest request,
        CancellationToken ct = default)
    {
        // Upload image to MinIO first
        if (request.Image == null || request.Image.Length == 0)
            return BadRequest(new { error = "Image file is required.", code = "IMAGE_REQUIRED" });

        if (request.Image.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = "File too large. Max 5 MB.", code = "FILE_TOO_LARGE" });

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(request.Image.ContentType))
            return BadRequest(new { error = "Invalid image type. Use JPEG, PNG, or WebP.", code = "INVALID_TYPE" });

        string imageUrl;
        using (var stream = request.Image.OpenReadStream())
        {
            imageUrl = await _imageUploadService.UploadAsync(stream, request.Image.FileName, "proposals", ct);
        }

        var command = new SendUniformProposalCommand(
            _currentUser.UserId,
            ChatChannelType.Contract,
            request.ChannelId,
            imageUrl,
            request.OutfitName
        );

        var result = await _sendProposalHandler.HandleAsync(command, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>School accepts a uniform proposal — creates outfit in school catalog.</summary>
    [HttpPost("proposals/{messageId:guid}/accept")]
    [Authorize(Roles = "School")]
    [ProducesResponseType(typeof(AcceptUniformProposalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptUniformProposal(Guid messageId, CancellationToken ct = default)
    {
        var result = await _acceptProposalHandler.HandleAsync(
            new AcceptUniformProposalCommand(_currentUser.UserId, messageId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }
}

public record SendMessageRequest(string Content);

public class SendUniformProposalRequest
{
    /// <summary>Contract channel ID</summary>
    public Guid ChannelId { get; set; }

    /// <summary>Proposed uniform name</summary>
    public string OutfitName { get; set; } = string.Empty;

    /// <summary>Uniform image file (jpg/png/webp, max 5MB)</summary>
    public IFormFile Image { get; set; } = null!;
}

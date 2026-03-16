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
    private readonly ICurrentUserService _currentUser;

    public ChatController(
        IGetChatMessagesQueryHandler getMessagesHandler,
        ISendChatMessageCommandHandler sendMessageHandler,
        ICurrentUserService currentUser)
    {
        _getMessagesHandler = getMessagesHandler;
        _sendMessageHandler = sendMessageHandler;
        _currentUser = currentUser;
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
}

public record SendMessageRequest(string Content);

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Feedbacks.Commands;
using VTOS.Application.Features.Feedbacks.DTOs;
using VTOS.Application.Features.Feedbacks.Queries;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/feedbacks")]
[Authorize(Roles = "Parent")]
public class FeedbacksController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubmitFeedbackCommandHandler _submitFeedbackHandler;
    private readonly IGetParentFeedbacksQueryHandler _getParentFeedbacksHandler;
    private readonly ILogger<FeedbacksController> _logger;

    public FeedbacksController(
        ICurrentUserService currentUserService,
        ISubmitFeedbackCommandHandler submitFeedbackHandler,
        IGetParentFeedbacksQueryHandler getParentFeedbacksHandler,
        ILogger<FeedbacksController> logger)
    {
        _currentUserService = currentUserService;
        _submitFeedbackHandler = submitFeedbackHandler;
        _getParentFeedbacksHandler = getParentFeedbacksHandler;
        _logger = logger;
    }

    /// <summary>
    /// Submit feedback for an order item
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/feedbacks/order-item
    ///     {
    ///         "orderItemId": "550e8400-e29b-41d4-a716-446655440000",
    ///         "rating": 5,
    ///         "comment": "Great quality and great fit!"
    ///     }
    /// </remarks>
    /// <param name="request">Feedback request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure result</returns>
    [HttpPost("order-item")]
    [ProducesResponseType(typeof(Result<SubmitFeedbackResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitOrderItemFeedback(
        [FromBody] SubmitFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request == null)
            {
                _logger.LogWarning("Feedback: Request is null");
                return BadRequest(Result<SubmitFeedbackResponse>.Failure("Request cannot be empty", "INVALID_REQUEST"));
            }

            _logger.LogInformation("Feedback submitted for order item: {OrderItemId} with rating: {Rating}",
                request.OrderItemId, request.Rating);

            var command = new SubmitFeedbackCommand(
                _currentUserService.UserId,
                request.OrderItemId,
                request.Rating,
                request.Comment
            );

            var result = await _submitFeedbackHandler.HandleAsync(command, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Feedback submission failed: {Error}", result.Error);
                return BadRequest(result);
            }

            _logger.LogInformation("Feedback submitted successfully: FeedbackId={FeedbackId}", result.Value?.FeedbackId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during feedback submission");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                Result<SubmitFeedbackResponse>.Failure(
                    "An unexpected error occurred during feedback submission",
                    "INTERNAL_SERVER_ERROR"));
        }
    }

    /// <summary>
    /// Get parent's feedbacks with filter options
    /// </summary>
    /// <remarks>
    /// Returns feedbacks submitted or available for rating, grouped by campaign with filter options.
    /// 
    /// Query parameters:
    /// - campaignId (optional): Filter by specific campaign
    /// - hasRating (optional): null/not provided = all, true = only rated, false = only not-yet-rated
    /// - page (default: 1)
    /// - pageSize (default: 10)
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ParentFeedbacksResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetParentFeedbacks(
        [FromQuery] Guid? campaignId = null,
        [FromQuery] bool? hasRating = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetParentFeedbacksQuery(
                _currentUserService.UserId,
                campaignId,
                hasRating,
                page,
                pageSize
            );

            var result = await _getParentFeedbacksHandler.HandleAsync(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting parent feedbacks");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An unexpected error occurred", code = "INTERNAL_SERVER_ERROR" });
        }
    }
}

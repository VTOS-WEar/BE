using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.SupportTickets;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/support-tickets")]
[Authorize(Roles = "Parent,Provider,School,HomeroomTeacher")]
public class SupportTicketsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly ICreateSupportTicketCommandHandler _createHandler;
    private readonly IGetMySupportTicketsQueryHandler _listHandler;
    private readonly IGetMySupportTicketDetailQueryHandler _detailHandler;
    private readonly IImageUploadService _imageUploadService;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".mp4", ".mov", ".avi"
    };

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public SupportTicketsController(
        ICurrentUserService currentUser,
        ICreateSupportTicketCommandHandler createHandler,
        IGetMySupportTicketsQueryHandler listHandler,
        IGetMySupportTicketDetailQueryHandler detailHandler,
        IImageUploadService imageUploadService)
    {
        _currentUser = currentUser;
        _createHandler = createHandler;
        _listHandler = listHandler;
        _detailHandler = detailHandler;
        _imageUploadService = imageUploadService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SupportTicketListResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _listHandler.HandleAsync(
            new GetMySupportTicketsQuery(_currentUser.UserId, page, pageSize, status),
            ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SupportTicketResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMineById(Guid id, CancellationToken ct = default)
    {
        var result = await _detailHandler.HandleAsync(
            new GetMySupportTicketDetailQuery(_currentUser.UserId, id),
            ct);

        if (!result.IsSuccess)
            return NotFound(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SupportTicketResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSupportTicketRequestDto request, CancellationToken ct = default)
    {
        var result = await _createHandler.HandleAsync(
            new CreateSupportTicketCommand(_currentUser.UserId, request),
            ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    /// <summary>Upload proof image/video for a support ticket. Returns the public URL.</summary>
    [HttpPost("upload-proof")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadProof(IFormFile file, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "File is required." });

        if (file.Length > MaxFileSize)
            return BadRequest(new { error = "File size exceeds 10 MB limit." });

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { error = $"File type '{ext}' is not allowed. Allowed: {string.Join(", ", AllowedExtensions)}" });

        await using var stream = file.OpenReadStream();
        var url = await _imageUploadService.UploadAsync(stream, file.FileName, "support-tickets", ct);

        return Ok(new { imageUrl = url });
    }
}

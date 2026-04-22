using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Schools.Commands;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Application.Features.Schools.Queries;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/schools/me/semester-publications")]
[Authorize(Roles = "School")]
public class SchoolSemesterPublicationsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly ICreateSemesterPublicationCommandHandler _createHandler;
    private readonly IUpdateSemesterPublicationCommandHandler _updateHandler;
    private readonly IDeleteDraftPublicationCommandHandler _deleteHandler;
    private readonly IPublishSemesterPublicationCommandHandler _publishHandler;
    private readonly ICloseSemesterPublicationCommandHandler _closeHandler;
    private readonly IGetSemesterPublicationsQueryHandler _listHandler;
    private readonly IGetSemesterPublicationDetailQueryHandler _detailHandler;
    private readonly IAddOutfitsToPublicationCommandHandler _addOutfitsHandler;
    private readonly IRemoveOutfitFromPublicationCommandHandler _removeOutfitHandler;
    private readonly IApproveProviderCommandHandler _approveProviderHandler;
    private readonly ISuspendProviderCommandHandler _suspendProviderHandler;
    private readonly IGetContractedOutfitSuggestionsQueryHandler _outfitSuggestionsHandler;
    private readonly IGetContractedProviderSuggestionsQueryHandler _providerSuggestionsHandler;

    public SchoolSemesterPublicationsController(
        ICurrentUserService currentUser,
        ICreateSemesterPublicationCommandHandler createHandler,
        IUpdateSemesterPublicationCommandHandler updateHandler,
        IDeleteDraftPublicationCommandHandler deleteHandler,
        IPublishSemesterPublicationCommandHandler publishHandler,
        ICloseSemesterPublicationCommandHandler closeHandler,
        IGetSemesterPublicationsQueryHandler listHandler,
        IGetSemesterPublicationDetailQueryHandler detailHandler,
        IAddOutfitsToPublicationCommandHandler addOutfitsHandler,
        IRemoveOutfitFromPublicationCommandHandler removeOutfitHandler,
        IApproveProviderCommandHandler approveProviderHandler,
        ISuspendProviderCommandHandler suspendProviderHandler,
        IGetContractedOutfitSuggestionsQueryHandler outfitSuggestionsHandler,
        IGetContractedProviderSuggestionsQueryHandler providerSuggestionsHandler)
    {
        _currentUser = currentUser;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _publishHandler = publishHandler;
        _closeHandler = closeHandler;
        _listHandler = listHandler;
        _detailHandler = detailHandler;
        _addOutfitsHandler = addOutfitsHandler;
        _removeOutfitHandler = removeOutfitHandler;
        _approveProviderHandler = approveProviderHandler;
        _suspendProviderHandler = suspendProviderHandler;
        _outfitSuggestionsHandler = outfitSuggestionsHandler;
        _providerSuggestionsHandler = providerSuggestionsHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetSemesterPublicationsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSemesterPublications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _listHandler.HandleAsync(
            new GetSemesterPublicationsQuery(_currentUser.UserId, page, pageSize, status), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SemesterPublicationDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSemesterPublicationDetail(Guid id, CancellationToken ct)
    {
        var result = await _detailHandler.HandleAsync(new GetSemesterPublicationDetailQuery(_currentUser.UserId, id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SemesterPublicationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSemesterPublication([FromBody] CreateSemesterPublicationRequest request, CancellationToken ct)
    {
        var result = await _createHandler.HandleAsync(
            new CreateSemesterPublicationCommand(
                _currentUser.UserId,
                request.Semester,
                request.AcademicYear,
                request.StartDate,
                request.EndDate,
                request.Description,
                request.Rules),
            ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SemesterPublicationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSemesterPublication(Guid id, [FromBody] UpdateSemesterPublicationRequest request, CancellationToken ct)
    {
        var result = await _updateHandler.HandleAsync(
            new UpdateSemesterPublicationCommand(
                _currentUser.UserId,
                id,
                request.Semester,
                request.AcademicYear,
                request.StartDate,
                request.EndDate,
                request.Description,
                request.Rules),
            ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSemesterPublication(Guid id, CancellationToken ct)
    {
        var result = await _deleteHandler.HandleAsync(new DeleteDraftPublicationCommand(_currentUser.UserId, id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(new { message = result.Value });
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(SemesterPublicationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PublishSemesterPublication(Guid id, CancellationToken ct)
    {
        var result = await _publishHandler.HandleAsync(new PublishSemesterPublicationCommand(_currentUser.UserId, id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(SemesterPublicationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CloseSemesterPublication(Guid id, CancellationToken ct)
    {
        var result = await _closeHandler.HandleAsync(new CloseSemesterPublicationCommand(_currentUser.UserId, id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/outfits")]
    [ProducesResponseType(typeof(SemesterPublicationDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddOutfits(Guid id, [FromBody] AddOutfitsRequest request, CancellationToken ct)
    {
        var result = await _addOutfitsHandler.HandleAsync(
            new AddOutfitsToPublicationCommand(_currentUser.UserId, id, request.OutfitIds, request.Notes), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}/outfits/{publicationOutfitId:guid}")]
    [ProducesResponseType(typeof(SemesterPublicationDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveOutfit(Guid id, Guid publicationOutfitId, CancellationToken ct)
    {
        var result = await _removeOutfitHandler.HandleAsync(
            new RemoveOutfitFromPublicationCommand(_currentUser.UserId, id, publicationOutfitId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/providers")]
    [ProducesResponseType(typeof(SemesterPublicationDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveProvider(Guid id, [FromBody] ApproveProviderRequest request, CancellationToken ct)
    {
        var result = await _approveProviderHandler.HandleAsync(
            new ApproveProviderCommand(_currentUser.UserId, id, request.ProviderID, request.ContractID), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/providers/{publicationProviderId:guid}/suspend")]
    [ProducesResponseType(typeof(SemesterPublicationDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SuspendProvider(Guid id, Guid publicationProviderId, [FromBody] SuspendProviderRequest request, CancellationToken ct)
    {
        var result = await _suspendProviderHandler.HandleAsync(
            new SuspendProviderCommand(_currentUser.UserId, id, publicationProviderId, request.Reason), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpGet("suggestions/outfits")]
    [ProducesResponseType(typeof(IReadOnlyList<ContractedOutfitSuggestionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOutfitSuggestions(CancellationToken ct)
    {
        var result = await _outfitSuggestionsHandler.HandleAsync(
            new GetContractedOutfitSuggestionsQuery(_currentUser.UserId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpGet("suggestions/providers")]
    [ProducesResponseType(typeof(IReadOnlyList<ContractedProviderSuggestionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviderSuggestions(CancellationToken ct)
    {
        var result = await _providerSuggestionsHandler.HandleAsync(
            new GetContractedProviderSuggestionsQuery(_currentUser.UserId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }
}

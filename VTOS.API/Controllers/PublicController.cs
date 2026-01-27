using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Features.Public.Queries;

namespace VTOS.API.Controllers;

/// <summary>
/// Public APIs for guest users (no authentication required).
/// UC-57: View School List
/// UC-58: View Uniform Categories
/// UC-59: View Uniform Details
/// </summary>
[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly GetSchoolsQueryHandler _getSchoolsHandler;
    private readonly GetCategoriesQueryHandler _getCategoriesHandler;
    private readonly GetOutfitDetailQueryHandler _getOutfitDetailHandler;

    public PublicController(
        GetSchoolsQueryHandler getSchoolsHandler,
        GetCategoriesQueryHandler getCategoriesHandler,
        GetOutfitDetailQueryHandler getOutfitDetailHandler)
    {
        _getSchoolsHandler = getSchoolsHandler;
        _getCategoriesHandler = getCategoriesHandler;
        _getOutfitDetailHandler = getOutfitDetailHandler;
    }

    /// <summary>
    /// UC-57: Get list of schools with search and pagination.
    /// </summary>
    /// <param name="search">Optional search keyword for school name</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    [HttpGet("schools")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchools(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = new GetSchoolsQuery(search, page, pageSize);
        var result = await _getSchoolsHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// UC-58: Get all uniform categories with outfit counts.
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken ct = default)
    {
        var query = new GetCategoriesQuery();
        var result = await _getCategoriesHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// UC-59: Get detailed information about a specific outfit.
    /// </summary>
    /// <param name="id">Outfit ID</param>
    [HttpGet("outfits/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOutfitDetail(Guid id, CancellationToken ct = default)
    {
        var query = new GetOutfitDetailQuery(id);
        var result = await _getOutfitDetailHandler.HandleAsync(query, ct);

        if (result == null)
            return NotFound(new { message = "Outfit not found" });

        return Ok(result);
    }
}

using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Features.Public.Queries;

namespace VTOS.API.Controllers;

/// <summary>
/// Public APIs for guest users (no authentication required).
/// UC 3.3.2: View School List
/// UC 3.3.3: View School Information
/// UC 3.3.4: View Uniform List
/// UC 3.3.5: View Uniform Detail
/// </summary>
[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly GetSchoolsQueryHandler _getSchoolsHandler;
    private readonly GetCategoriesQueryHandler _getCategoriesHandler;
    private readonly GetOutfitDetailQueryHandler _getOutfitDetailHandler;
    private readonly GetSchoolDetailQueryHandler _getSchoolDetailHandler;
    private readonly GetUniformListQueryHandler _getUniformListHandler;

    public PublicController(
        GetSchoolsQueryHandler getSchoolsHandler,
        GetCategoriesQueryHandler getCategoriesHandler,
        GetOutfitDetailQueryHandler getOutfitDetailHandler,
        GetSchoolDetailQueryHandler getSchoolDetailHandler,
        GetUniformListQueryHandler getUniformListHandler)
    {
        _getSchoolsHandler = getSchoolsHandler;
        _getCategoriesHandler = getCategoriesHandler;
        _getOutfitDetailHandler = getOutfitDetailHandler;
        _getSchoolDetailHandler = getSchoolDetailHandler;
        _getUniformListHandler = getUniformListHandler;
    }

    /// <summary>
    /// UC 3.3.2: Get list of schools with search and pagination.
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
    /// UC 3.3.3: Get detailed information about a specific school.
    /// Includes active campaigns, outfit count, and contact info.
    /// Response is cached for 5 minutes to reduce DB load.
    /// </summary>
    /// <param name="id">School ID</param>
    [HttpGet("schools/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSchoolDetail(Guid id, CancellationToken ct = default)
    {
        var query = new GetSchoolDetailQuery(id);
        var result = await _getSchoolDetailHandler.HandleAsync(query, ct);

        if (result == null)
            return NotFound(new { message = "School not found" });

        return Ok(result);
    }

    /// <summary>
    /// UC 3.3.4: Get paginated list of uniforms for a specific school.
    /// Only returns available, non-deleted outfits. Includes categories and ratings.
    /// Response is cached for 5 minutes to reduce DB load.
    /// </summary>
    /// <param name="schoolId">School ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    [HttpGet("schools/{schoolId:guid}/uniforms")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUniformList(
        Guid schoolId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = new GetUniformListQuery(schoolId, page, pageSize);
        var result = await _getUniformListHandler.HandleAsync(query, ct);

        if (result == null)
            return NotFound(new { message = "School not found" });

        return Ok(result);
    }

    /// <summary>
    /// Get all uniform categories with outfit counts.
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
    /// UC 3.3.5: Get detailed information about a specific outfit.
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

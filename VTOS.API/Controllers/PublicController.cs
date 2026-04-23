using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Features.Public.Queries;
using VTOS.Application.Features.Providers.DTOs;

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
    private readonly PublicSearchQueryHandler _publicSearchHandler;
    private readonly GetUniformWarehouseQueryHandler _getUniformWarehouseHandler;
    private readonly GetSchoolSemesterCatalogQueryHandler _getSchoolSemesterCatalogHandler;
    private readonly GetAllSchoolSemesterCatalogsQueryHandler _getAllSchoolSemesterCatalogsHandler;
    private readonly GetProvidersForPublicationOutfitQueryHandler _getProvidersForPublicationOutfitHandler;
    private readonly GetProviderPublicProfileQueryHandler _getProviderPublicProfileHandler;
    private readonly GetProviderRatingsQueryHandler _getProviderRatingsHandler;
    private readonly GetProviderRankingQueryHandler _getProviderRankingHandler;

    public PublicController(
        GetSchoolsQueryHandler getSchoolsHandler,
        GetCategoriesQueryHandler getCategoriesHandler,
        GetOutfitDetailQueryHandler getOutfitDetailHandler,
        GetSchoolDetailQueryHandler getSchoolDetailHandler,
        GetUniformListQueryHandler getUniformListHandler,
        PublicSearchQueryHandler publicSearchHandler,
        GetUniformWarehouseQueryHandler getUniformWarehouseHandler,
        GetSchoolSemesterCatalogQueryHandler getSchoolSemesterCatalogHandler,
        GetAllSchoolSemesterCatalogsQueryHandler getAllSchoolSemesterCatalogsHandler,
        GetProvidersForPublicationOutfitQueryHandler getProvidersForPublicationOutfitHandler,
        GetProviderPublicProfileQueryHandler getProviderPublicProfileHandler,
        GetProviderRatingsQueryHandler getProviderRatingsHandler,
        GetProviderRankingQueryHandler getProviderRankingHandler)
    {
        _getSchoolsHandler = getSchoolsHandler;
        _getCategoriesHandler = getCategoriesHandler;
        _getOutfitDetailHandler = getOutfitDetailHandler;
        _getSchoolDetailHandler = getSchoolDetailHandler;
        _getUniformListHandler = getUniformListHandler;
        _publicSearchHandler = publicSearchHandler;
        _getUniformWarehouseHandler = getUniformWarehouseHandler;
        _getSchoolSemesterCatalogHandler = getSchoolSemesterCatalogHandler;
        _getAllSchoolSemesterCatalogsHandler = getAllSchoolSemesterCatalogsHandler;
        _getProvidersForPublicationOutfitHandler = getProvidersForPublicationOutfitHandler;
        _getProviderPublicProfileHandler = getProviderPublicProfileHandler;
        _getProviderRatingsHandler = getProviderRatingsHandler;
        _getProviderRankingHandler = getProviderRankingHandler;
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
    /// Unified search across schools and uniforms.
    /// Returns schools and uniforms matching the query (case-insensitive contains).
    /// Used by the navbar search bar.
    /// </summary>
    /// <param name="q">Search keyword</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per section (default: 10)</param>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = new PublicSearchQuery(q, page, pageSize);
        var result = await _publicSearchHandler.HandleAsync(query, ct);
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
    /// Get detailed information about a specific outfit.
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

    /// <summary>
    /// Get summary data for the Uniform Warehouse page.
    /// Includes active campaigns, featured outfits, and all available outfits.
    /// </summary>
    [HttpGet("uniform-warehouse")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUniformWarehouse(
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var query = new GetUniformWarehouseQuery(pageSize);
        var result = await _getUniformWarehouseHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("schools/{schoolId:guid}/semester-catalog")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSchoolSemesterCatalog(Guid schoolId, CancellationToken ct = default)
    {
        var result = await _getSchoolSemesterCatalogHandler.HandleAsync(new GetSchoolSemesterCatalogQuery(schoolId), ct);
        if (result == null)
            return NotFound(new { message = "Active semester catalog not found" });
        return Ok(result);
    }

    [HttpGet("schools/{schoolId:guid}/semester-catalogs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSchoolSemesterCatalogs(Guid schoolId, CancellationToken ct = default)
    {
        var result = await _getAllSchoolSemesterCatalogsHandler.HandleAsync(new GetAllSchoolSemesterCatalogsQuery(schoolId), ct);
        return Ok(result);
    }

    [HttpGet("semester-publications/{publicationId:guid}/outfits/{outfitId:guid}/providers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProvidersForPublicationOutfit(Guid publicationId, Guid outfitId, CancellationToken ct = default)
    {
        var result = await _getProvidersForPublicationOutfitHandler.HandleAsync(
            new GetProvidersForPublicationOutfitQuery(publicationId, outfitId), ct);
        if (result == null)
            return NotFound(new { message = "Publication or outfit not found" });
        return Ok(result);
    }

    [HttpGet("providers/{providerId:guid}/profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProviderPublicProfile(Guid providerId, CancellationToken ct = default)
    {
        var result = await _getProviderPublicProfileHandler.HandleAsync(new GetProviderPublicProfileQuery(providerId), ct);
        if (result == null)
            return NotFound(new { message = "Provider not found" });
        return Ok(result);
    }

    [HttpGet("providers/{providerId:guid}/ratings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProviderRatings(Guid providerId, CancellationToken ct = default)
    {
        var result = await _getProviderRatingsHandler.HandleAsync(new GetProviderRatingsQuery(providerId), ct);
        if (result == null)
            return NotFound(new { message = "Provider not found" });
        return Ok(result);
    }

    [HttpGet("schools/{schoolId:guid}/provider-ranking")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProviderRanking(Guid schoolId, CancellationToken ct = default)
    {
        var result = await _getProviderRankingHandler.HandleAsync(new GetProviderRankingQuery(schoolId), ct);
        if (result == null)
            return NotFound(new { message = "School not found" });
        return Ok(result);
    }
}

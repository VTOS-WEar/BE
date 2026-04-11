namespace VTOS.Application.Features.Public.DTOs;

/// <summary>
/// Response DTO for the unified public search endpoint.
/// Returns schools and uniforms matching the query.
/// </summary>
public class PublicSearchResponse
{
    public List<SchoolSearchResult> Schools { get; set; } = new();
    public List<UniformSearchResult> Uniforms { get; set; } = new();
    public int TotalSchools { get; set; }
    public int TotalUniforms { get; set; }
}

public class SchoolSearchResult
{
    public Guid Id { get; set; }
    public string SchoolName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Address { get; set; }
    public int UniformCount { get; set; }
}

public class UniformSearchResult
{
    public Guid Id { get; set; }
    public string OutfitName { get; set; } = string.Empty;
    public string? MainImageUrl { get; set; }
    public decimal Price { get; set; }
    public string SchoolName { get; set; } = string.Empty;
    public Guid SchoolId { get; set; }
}

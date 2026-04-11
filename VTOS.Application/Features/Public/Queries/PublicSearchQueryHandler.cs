using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Public.DTOs;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Public.Queries;

/// <summary>
/// Unified search across schools and uniforms.
/// Returns schools and uniforms matching the search query (case-insensitive contains).
/// No authentication required.
/// </summary>
public class PublicSearchQueryHandler
{
    private readonly IApplicationDbContext _db;

    public PublicSearchQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PublicSearchResponse> HandleAsync(PublicSearchQuery query, CancellationToken ct = default)
    {
        var searchTerm = query.Q?.Trim();
        var isEmpty = string.IsNullOrWhiteSpace(searchTerm);

        // ── Schools ──────────────────────────────────────────────────────────
        IQueryable<School> schoolQuery = _db.Schools.AsNoTracking();

        if (!isEmpty)
        {
            var term = searchTerm.ToLower();
            schoolQuery = schoolQuery.Where(s =>
                EF.Functions.Like(s.SchoolName.ToLower(), $"%{term}%")
                || (s.ContactInfo != null && EF.Functions.Like(s.ContactInfo.ToLower(), $"%{term}%"))
            );
        }

        var schools = await schoolQuery
            .OrderBy(s => s.SchoolName)
            .Take(query.PageSize)
            .Select(s => new SchoolSearchResult
            {
                Id = s.Id,
                SchoolName = s.SchoolName,
                LogoUrl = s.LogoURL,
                Address = s.ContactInfo,   // raw JSON — parsed below
                UniformCount = s.Outfits.Count(o => !o.IsDeleted)
            })
            .ToListAsync(ct);

        // ContactInfo is stored as JSON: {"email":"...","phone":"...","address":"...","foundedYear":...}
        // Extract just the address string for display — avoid showing raw JSON in the UI
        foreach (var school in schools)
            school.Address = ExtractAddress(school.Address);

        var totalSchools = await schoolQuery.CountAsync(ct);

        // ── Uniforms ─────────────────────────────────────────────────────────
        IQueryable<Outfit> outfitQuery = _db.Outfits
            .AsNoTracking()
            .Where(o => !o.IsDeleted && o.IsAvailable);

        if (!isEmpty)
        {
            var term = searchTerm.ToLower();
            outfitQuery = outfitQuery.Where(o =>
                EF.Functions.Like(o.OutfitName.ToLower(), $"%{term}%")
                || (o.Description != null && EF.Functions.Like(o.Description.ToLower(), $"%{term}%"))
            );
        }

        var uniforms = await outfitQuery
            .Include(o => o.School)
            .OrderBy(o => o.OutfitName)
            .Take(query.PageSize)
            .Select(o => new UniformSearchResult
            {
                Id = o.Id,
                OutfitName = o.OutfitName,
                MainImageUrl = o.MainImageURL,
                Price = o.Price,
                SchoolName = o.School.SchoolName,
                SchoolId = o.SchoolID
            })
            .ToListAsync(ct);

        var totalUniforms = await outfitQuery.CountAsync(ct);

        return new PublicSearchResponse
        {
            Schools = schools,
            Uniforms = uniforms,
            TotalSchools = totalSchools,
            TotalUniforms = totalUniforms
        };
    }

    /// <summary>
    /// Extracts the "address" field from a ContactInfo JSON string.
    /// Returns the raw value as-is if it's not JSON (plain text address).
    /// Returns null if the input is null or the address field is missing.
    /// </summary>
    private static string? ExtractAddress(string? contactInfo)
    {
        if (string.IsNullOrWhiteSpace(contactInfo)) return null;
        if (!contactInfo.TrimStart().StartsWith('{')) return contactInfo; // plain text, use as-is

        try
        {
            using var doc = JsonDocument.Parse(contactInfo);
            if (doc.RootElement.TryGetProperty("address", out var addressProp))
                return addressProp.GetString();
        }
        catch (JsonException) { /* malformed JSON — fall through */ }

        return null;
    }
}

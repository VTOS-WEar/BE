using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Application.Features.Schools.Helpers;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Partial-update a ProductVariant. Validates school ownership.
/// </summary>
public class UpdateVariantCommandHandler : IUpdateVariantCommandHandler
{
    private readonly IApplicationDbContext _db;

    public UpdateVariantCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ProductVariantDto>> HandleAsync(UpdateVariantCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user == null)
            return Result<ProductVariantDto>.Failure("User not found.", "USER_NOT_FOUND");

        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        if (schoolMgr == null)
            return Result<ProductVariantDto>.Failure("School profile not set up yet.", "SCHOOL_NOT_FOUND");

        // Verify outfit belongs to this school
        var outfit = await _db.Outfits
            .FirstOrDefaultAsync(o => o.Id == command.OutfitId && !o.IsDeleted, ct);

        if (outfit == null)
            return Result<ProductVariantDto>.Failure("Outfit not found.", "OUTFIT_NOT_FOUND");

        if (outfit.SchoolID != schoolMgr.SchoolID)
            return Result<ProductVariantDto>.Failure("You do not have permission to modify this outfit.", "OUTFIT_NOT_FOUND");

        // Find the variant
        var variant = await _db.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == command.VariantId && v.OutfitID == command.OutfitId && !v.IsDeleted, ct);

        if (variant == null)
            return Result<ProductVariantDto>.Failure("Variant not found.", "VARIANT_NOT_FOUND");

        // Check duplicate size if size is being changed
        if (command.Size != null && command.Size != variant.Size)
        {
            var duplicateSize = await _db.ProductVariants
                .AnyAsync(v => v.OutfitID == command.OutfitId && !v.IsDeleted && v.Size == command.Size && v.Id != command.VariantId, ct);

            if (duplicateSize)
                return Result<ProductVariantDto>.Failure($"Size '{command.Size}' already exists for this outfit.", "DUPLICATE_SIZE");
        }

        // Apply partial updates
        var originalSize = variant.Size;
        if (command.Size != null) variant.Size = command.Size;
        if (command.ColorVariant != null) variant.ColorVariant = command.ColorVariant;
        if (command.MaterialType != null) variant.MaterialType = command.MaterialType;
        if (command.SKUCode != null) variant.SKUCode = command.SKUCode;

        var activeSize = variant.Size;
        var detail = await GetOrMoveSizeChartDetailAsync(outfit, originalSize, activeSize, ct);
        var existingMeasurements = await _db.SizeChartMeasurements
            .Where(m => m.SizeChartDetailId == detail.Id)
            .ToListAsync(ct);

        var measurements = command.Measurements != null
            ? UpsertMeasurements(detail.Id, command.Measurements, existingMeasurements)
            : existingMeasurements;

        int retries = 3;
        bool saveSuccess = false;
        while (retries > 0 && !saveSuccess)
        {
            try
            {
                await _db.SaveChangesAsync(ct);
                saveSuccess = true;
            }
            catch (DbUpdateConcurrencyException)
            {
                retries--;
                if (retries == 0) throw;

                // Reload fresh data from DB to reconcile again
                var entry = ((DbContext)_db).Entry(detail);
                await entry.ReloadAsync(ct);
                await entry.Collection("Measurements").LoadAsync(ct);
                
                // Re-run reconciliation with latest database state
                measurements = command.Measurements != null
                    ? UpsertMeasurements(detail.Id, command.Measurements, detail.Measurements.ToList())
                    : detail.Measurements.ToList();
            }
        }

        return Result<ProductVariantDto>.Success(new ProductVariantDto
        {
            ProductVariantId = variant.Id,
            OutfitId = variant.OutfitID,
            Size = variant.Size,
            Price = variant.Price,
            StockQuantity = variant.StockQuantity,
            ColorVariant = variant.ColorVariant,
            MaterialType = variant.MaterialType,
            SKUCode = variant.SKUCode,
            VariantImageURL = variant.VariantImageURL,
            Measurements = VariantSizeChartSyncHelper.ToDtos(measurements),
        });
    }

    private async Task<SizeChartDetail> GetOrMoveSizeChartDetailAsync(Outfit outfit, string originalSize, string activeSize, CancellationToken ct)
    {
        var normalizedLabel = activeSize.Trim();

        if (outfit.SizeChartID == null)
        {
            var chart = new SizeChart
            {
                Id = Guid.NewGuid(),
                ChartName = $"{outfit.OutfitName} size chart",
                Unit = "cm",
            };
            outfit.SizeChartID = chart.Id;
            _db.SizeCharts.Add(chart);
        }

        var existingDetail = await _db.SizeChartDetails
            .Include(d => d.Measurements)
            .FirstOrDefaultAsync(d => d.SizeChartID == outfit.SizeChartID && d.SizeLabel == normalizedLabel, ct);

        if (existingDetail != null)
        {
            return existingDetail;
        }

        if (!string.Equals(originalSize, activeSize, StringComparison.OrdinalIgnoreCase))
        {
            var oldDetail = await _db.SizeChartDetails
                .Include(d => d.Measurements)
                .FirstOrDefaultAsync(d => d.SizeChartID == outfit.SizeChartID && d.SizeLabel == originalSize, ct);

            if (oldDetail != null)
            {
                oldDetail.SizeLabel = normalizedLabel;
                return oldDetail;
            }
        }

        var detail = new SizeChartDetail
        {
            Id = Guid.NewGuid(),
            SizeChartID = outfit.SizeChartID!.Value,
            SizeLabel = normalizedLabel,
        };

        _db.SizeChartDetails.Add(detail);
        return detail;
    }

    private List<SizeChartMeasurement> UpsertMeasurements(
        Guid sizeChartDetailId,
        IEnumerable<VariantMeasurementInputDto>? inputs,
        List<SizeChartMeasurement> existingMeasurements)
    {
        var normalized = VariantSizeChartSyncHelper.NormalizeInputs(inputs);
        var existingByKey = existingMeasurements.ToDictionary(m => m.FieldKey, StringComparer.OrdinalIgnoreCase);

        foreach (var measurement in existingMeasurements)
        {
            if (normalized.All(input => !string.Equals(input.FieldKey, measurement.FieldKey, StringComparison.OrdinalIgnoreCase)))
            {
                _db.SizeChartMeasurements.Remove(measurement);
            }
        }

        var result = new List<SizeChartMeasurement>();
        foreach (var input in normalized)
        {
            if (existingByKey.TryGetValue(input.FieldKey, out var current))
            {
                current.DisplayName = input.DisplayName;
                current.Unit = string.IsNullOrWhiteSpace(input.Unit) ? "cm" : input.Unit.Trim();
                current.MinCm = input.MinCm;
                current.MaxCm = input.MaxCm;
                result.Add(current);
                continue;
            }

            var created = new SizeChartMeasurement
            {
                Id = Guid.NewGuid(),
                SizeChartDetailId = sizeChartDetailId,
                FieldKey = input.FieldKey,
                DisplayName = input.DisplayName,
                Unit = string.IsNullOrWhiteSpace(input.Unit) ? "cm" : input.Unit.Trim(),
                MinCm = input.MinCm,
                MaxCm = input.MaxCm,
            };
            _db.SizeChartMeasurements.Add(created);
            result.Add(created);
        }

        return result;
    }
}

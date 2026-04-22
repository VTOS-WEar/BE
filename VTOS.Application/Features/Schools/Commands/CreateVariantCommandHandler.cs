using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Application.Features.Schools.Helpers;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Creates a new ProductVariant for an outfit owned by the current school.
/// Validates school ownership and duplicate size check.
/// </summary>
public class CreateVariantCommandHandler : ICreateVariantCommandHandler
{
    private readonly IApplicationDbContext _db;

    public CreateVariantCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ProductVariantDto>> HandleAsync(CreateVariantCommand command, CancellationToken ct = default)
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

        // Check for duplicate size
        var duplicateSize = await _db.ProductVariants
            .AnyAsync(v => v.OutfitID == command.OutfitId && !v.IsDeleted && v.Size == command.Size, ct);

        if (duplicateSize)
            return Result<ProductVariantDto>.Failure($"Size '{command.Size}' already exists for this outfit.", "DUPLICATE_SIZE");

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            OutfitID = command.OutfitId,
            Size = command.Size.Trim(),
            Price = outfit.Price, // Inherit price from Outfit
            StockQuantity = 0, // Managed by Provider later
            ColorVariant = string.IsNullOrWhiteSpace(command.ColorVariant) ? null : command.ColorVariant.Trim(),
            MaterialType = string.IsNullOrWhiteSpace(command.MaterialType) ? null : command.MaterialType.Trim(),
            SKUCode = string.IsNullOrWhiteSpace(command.SKUCode) ? null : command.SKUCode.Trim(),
            IsDeleted = false,
        };

        _db.ProductVariants.Add(variant);

        var detail = await EnsureSizeChartDetailAsync(outfit, variant.Size, ct);
        var measurements = UpsertMeasurements(detail.Id, command.Measurements, null);

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

                // Reload SizeChartDetail so we can re-sync measurements
                var entry = ((DbContext)_db).Entry(detail);
                await entry.ReloadAsync(ct);
                await entry.Collection("Measurements").LoadAsync(ct);

                // Re-run the reconciliation logic with the latest measurement list from DB
                measurements = UpsertMeasurements(detail.Id, command.Measurements, detail.Measurements.ToList());
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

    private async Task<SizeChartDetail> EnsureSizeChartDetailAsync(Outfit outfit, string sizeLabel, CancellationToken ct)
    {
        var normalizedLabel = sizeLabel.Trim();

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
        List<SizeChartMeasurement>? existingMeasurements)
    {
        var normalized = VariantSizeChartSyncHelper.NormalizeInputs(inputs);
        var existing = existingMeasurements ?? new List<SizeChartMeasurement>();
        var existingByKey = existing.ToDictionary(m => m.FieldKey, StringComparer.OrdinalIgnoreCase);

        foreach (var measurement in existing)
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

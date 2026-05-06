using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Application.Features.Schools.Helpers;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Commands;

public record GetProviderCatalogVariantsQuery(Guid UserId, Guid SemesterPublicationProviderId, Guid OutfitId);

public interface IGetProviderCatalogVariantsQueryHandler
{
    Task<Result<List<ProductVariantDto>>> HandleAsync(GetProviderCatalogVariantsQuery query, CancellationToken ct = default);
}

public record CreateProviderCatalogVariantCommand(
    Guid UserId,
    Guid SemesterPublicationProviderId,
    Guid OutfitId,
    string Size,
    string? ColorVariant,
    string? MaterialType,
    string? SKUCode,
    IReadOnlyCollection<VariantMeasurementInputDto>? Measurements);

public interface ICreateProviderCatalogVariantCommandHandler
{
    Task<Result<List<ProductVariantDto>>> HandleAsync(CreateProviderCatalogVariantCommand command, CancellationToken ct = default);
}

public record UpdateProviderCatalogVariantCommand(
    Guid UserId,
    Guid SemesterPublicationProviderId,
    Guid OutfitId,
    Guid VariantId,
    string? Size,
    string? ColorVariant,
    string? MaterialType,
    string? SKUCode,
    IReadOnlyCollection<VariantMeasurementInputDto>? Measurements);

public interface IUpdateProviderCatalogVariantCommandHandler
{
    Task<Result<List<ProductVariantDto>>> HandleAsync(UpdateProviderCatalogVariantCommand command, CancellationToken ct = default);
}

public record DeleteProviderCatalogVariantCommand(Guid UserId, Guid SemesterPublicationProviderId, Guid OutfitId, Guid VariantId);

public interface IDeleteProviderCatalogVariantCommandHandler
{
    Task<Result<List<ProductVariantDto>>> HandleAsync(DeleteProviderCatalogVariantCommand command, CancellationToken ct = default);
}

public class ProviderCatalogVariantCommandHandler :
    IGetProviderCatalogVariantsQueryHandler,
    ICreateProviderCatalogVariantCommandHandler,
    IUpdateProviderCatalogVariantCommandHandler,
    IDeleteProviderCatalogVariantCommandHandler
{
    private readonly IApplicationDbContext _db;

    public ProviderCatalogVariantCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<ProductVariantDto>>> HandleAsync(GetProviderCatalogVariantsQuery query, CancellationToken ct = default)
    {
        var context = await ResolveContextAsync(query.UserId, query.SemesterPublicationProviderId, query.OutfitId, ensureCatalogItem: false, ct);
        if (!context.IsSuccess)
            return Result<List<ProductVariantDto>>.Failure(context.Error!, context.ErrorCode);

        var variants = await GetVisibleVariantsAsync(context.Value!, ct);
        return Result<List<ProductVariantDto>>.Success(variants);
    }

    public async Task<Result<List<ProductVariantDto>>> HandleAsync(CreateProviderCatalogVariantCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Size))
            return Result<List<ProductVariantDto>>.Failure("Size is required.", "SIZE_REQUIRED");

        var contextResult = await ResolveContextAsync(command.UserId, command.SemesterPublicationProviderId, command.OutfitId, ensureCatalogItem: true, ct);
        if (!contextResult.IsSuccess)
            return Result<List<ProductVariantDto>>.Failure(contextResult.Error!, contextResult.ErrorCode);

        var context = contextResult.Value!;
        await EnsureProviderVariantsAsync(context, ct);
        var size = command.Size.Trim();

        var duplicate = await _db.ProductVariants.AnyAsync(v =>
            v.ProviderCatalogItemID == context.CatalogItem!.Id &&
            !v.IsDeleted &&
            v.Size == size,
            ct);
        if (duplicate)
            return Result<List<ProductVariantDto>>.Failure($"Size '{size}' already exists for this catalog item.", "DUPLICATE_SIZE");

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            OutfitID = context.Outfit.Id,
            ProviderCatalogItemID = context.CatalogItem!.Id,
            Size = size,
            Price = context.CatalogItem.PublicationPrice,
            StockQuantity = 0,
            ColorVariant = TrimOrNull(command.ColorVariant),
            MaterialType = TrimOrNull(command.MaterialType),
            SKUCode = TrimOrNull(command.SKUCode),
            IsDeleted = false
        };
        _db.ProductVariants.Add(variant);

        var detail = await EnsureSizeChartDetailAsync(context.CatalogItem, context.Outfit, size, ct);
        UpsertMeasurements(detail.Id, command.Measurements, detail.Measurements.ToList());

        await _db.SaveChangesAsync(ct);
        return Result<List<ProductVariantDto>>.Success(await GetVisibleVariantsAsync(context, ct));
    }

    public async Task<Result<List<ProductVariantDto>>> HandleAsync(UpdateProviderCatalogVariantCommand command, CancellationToken ct = default)
    {
        var contextResult = await ResolveContextAsync(command.UserId, command.SemesterPublicationProviderId, command.OutfitId, ensureCatalogItem: true, ct);
        if (!contextResult.IsSuccess)
            return Result<List<ProductVariantDto>>.Failure(contextResult.Error!, contextResult.ErrorCode);

        var context = contextResult.Value!;
        await EnsureProviderVariantsAsync(context, ct);
        var variant = await FindProviderVariantAsync(context, command.VariantId, ct);
        if (variant == null)
            return Result<List<ProductVariantDto>>.Failure("Variant not found.", "VARIANT_NOT_FOUND");

        var originalSize = variant.Size;
        if (command.Size != null)
        {
            var size = command.Size.Trim();
            if (string.IsNullOrWhiteSpace(size))
                return Result<List<ProductVariantDto>>.Failure("Size is required.", "SIZE_REQUIRED");

            if (!string.Equals(size, variant.Size, StringComparison.OrdinalIgnoreCase))
            {
                var duplicate = await _db.ProductVariants.AnyAsync(v =>
                    v.ProviderCatalogItemID == context.CatalogItem!.Id &&
                    !v.IsDeleted &&
                    v.Size == size &&
                    v.Id != variant.Id,
                    ct);
                if (duplicate)
                    return Result<List<ProductVariantDto>>.Failure($"Size '{size}' already exists for this catalog item.", "DUPLICATE_SIZE");
            }

            variant.Size = size;
        }

        if (command.ColorVariant != null) variant.ColorVariant = TrimOrNull(command.ColorVariant);
        if (command.MaterialType != null) variant.MaterialType = TrimOrNull(command.MaterialType);
        if (command.SKUCode != null) variant.SKUCode = TrimOrNull(command.SKUCode);

        var detail = await GetOrMoveSizeChartDetailAsync(context.CatalogItem!, context.Outfit, originalSize, variant.Size, ct);
        if (command.Measurements != null)
        {
            var existing = await _db.SizeChartMeasurements
                .Where(m => m.SizeChartDetailId == detail.Id)
                .ToListAsync(ct);
            UpsertMeasurements(detail.Id, command.Measurements, existing);
        }

        await _db.SaveChangesAsync(ct);
        return Result<List<ProductVariantDto>>.Success(await GetVisibleVariantsAsync(context, ct));
    }

    public async Task<Result<List<ProductVariantDto>>> HandleAsync(DeleteProviderCatalogVariantCommand command, CancellationToken ct = default)
    {
        var contextResult = await ResolveContextAsync(command.UserId, command.SemesterPublicationProviderId, command.OutfitId, ensureCatalogItem: true, ct);
        if (!contextResult.IsSuccess)
            return Result<List<ProductVariantDto>>.Failure(contextResult.Error!, contextResult.ErrorCode);

        var context = contextResult.Value!;
        await EnsureProviderVariantsAsync(context, ct);
        var variant = await FindProviderVariantAsync(context, command.VariantId, ct);
        if (variant == null)
            return Result<List<ProductVariantDto>>.Failure("Variant not found.", "VARIANT_NOT_FOUND");

        variant.IsDeleted = true;
        if (context.CatalogItem!.SizeChartID.HasValue)
        {
            var detail = await _db.SizeChartDetails
                .Include(d => d.Measurements)
                .FirstOrDefaultAsync(d => d.SizeChartID == context.CatalogItem.SizeChartID && d.SizeLabel == variant.Size, ct);
            if (detail != null)
                _db.SizeChartDetails.Remove(detail);
        }

        await _db.SaveChangesAsync(ct);
        return Result<List<ProductVariantDto>>.Success(await GetVisibleVariantsAsync(context, ct));
    }

    private async Task<Result<ProviderCatalogVariantContext>> ResolveContextAsync(
        Guid userId,
        Guid semesterPublicationProviderId,
        Guid outfitId,
        bool ensureCatalogItem,
        CancellationToken ct)
    {
        var providerId = await _db.ProviderManagers
            .AsNoTracking()
            .Where(x => x.UserID == userId)
            .Select(x => (Guid?)x.ProviderID)
            .FirstOrDefaultAsync(ct);

        if (!providerId.HasValue)
            return Result<ProviderCatalogVariantContext>.Failure("Provider not found for current user.", "PROVIDER_NOT_FOUND");

        var now = DateTime.UtcNow;
        var usableContractStatuses = new[] { "Active", "InUse" };
        var publicationProvider = await _db.SemesterPublicationProviders
            .Include(x => x.SemesterPublication)
            .Include(x => x.Contract)
            .FirstOrDefaultAsync(x =>
                x.Id == semesterPublicationProviderId &&
                x.ProviderID == providerId.Value,
                ct);

        if (publicationProvider == null)
            return Result<ProviderCatalogVariantContext>.Failure("Publication provider assignment not found.", "PUBLICATION_PROVIDER_NOT_FOUND");

        if (publicationProvider.Status == SemPublicationProviderStatus.Suspended)
            return Result<ProviderCatalogVariantContext>.Failure("Suspended providers cannot update catalog sizes for this publication.", "PROVIDER_SUSPENDED");

        if (!publicationProvider.ContractID.HasValue || publicationProvider.Contract == null)
            return Result<ProviderCatalogVariantContext>.Failure("A linked supplier agreement is required before catalog sizes can be managed.", "CONTRACT_REQUIRED");

        if (!usableContractStatuses.Contains(publicationProvider.Contract.Status) || publicationProvider.Contract.ExpiresAt <= now)
            return Result<ProviderCatalogVariantContext>.Failure("The linked supplier agreement has expired or is not active.", "CONTRACT_NOT_ACTIVE");

        var outfit = await _db.Outfits
            .Include(o => o.SizeChart)
                .ThenInclude(sc => sc!.SizeChartDetails)
                    .ThenInclude(d => d.Measurements)
            .FirstOrDefaultAsync(o => o.Id == outfitId && !o.IsDeleted, ct);

        if (outfit == null)
            return Result<ProviderCatalogVariantContext>.Failure("Outfit not found.", "OUTFIT_NOT_FOUND");

        var publicationOutfitExists = await _db.SemesterPublicationOutfits.AnyAsync(x =>
            x.SemesterPublicationID == publicationProvider.SemesterPublicationID &&
            x.OutfitID == outfitId,
            ct);
        if (!publicationOutfitExists)
            return Result<ProviderCatalogVariantContext>.Failure("This outfit is not part of the selected semester publication.", "OUTFIT_NOT_IN_PUBLICATION");

        var contractItem = await _db.ContractItems.FirstOrDefaultAsync(x =>
            x.ContractID == publicationProvider.ContractID.Value &&
            x.OutfitID == outfitId,
            ct);
        if (contractItem == null)
            return Result<ProviderCatalogVariantContext>.Failure("The linked supplier agreement does not include this outfit.", "CONTRACT_ITEM_NOT_FOUND");

        var catalogItem = await _db.ProviderCatalogItems
            .Include(x => x.SizeChart)
                .ThenInclude(sc => sc!.SizeChartDetails)
                    .ThenInclude(d => d.Measurements)
            .Include(x => x.ProductVariants)
            .FirstOrDefaultAsync(x =>
                x.SemesterPublicationProviderID == publicationProvider.Id &&
                x.ContractItemID == contractItem.Id,
                ct);

        if (catalogItem == null && ensureCatalogItem)
        {
            catalogItem = new ProviderCatalogItem
            {
                Id = Guid.NewGuid(),
                ProviderID = providerId.Value,
                SemesterPublicationProviderID = publicationProvider.Id,
                ContractItemID = contractItem.Id,
                OutfitID = outfitId,
                DisplayName = outfit.OutfitName,
                ShortDescription = outfit.Description,
                PublicationPrice = contractItem.PricePerUnit,
                PostDeadlinePrice = contractItem.PricePerUnit,
                MainImageUrl = outfit.MainImageURL,
                Status = ProviderCatalogItemStatus.Draft,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.ProviderCatalogItems.Add(catalogItem);
        }

        return Result<ProviderCatalogVariantContext>.Success(new ProviderCatalogVariantContext(providerId.Value, publicationProvider, outfit, contractItem, catalogItem));
    }

    private async Task EnsureProviderVariantsAsync(ProviderCatalogVariantContext context, CancellationToken ct)
    {
        var catalogItem = context.CatalogItem ?? throw new InvalidOperationException("Catalog item is required.");
        var hasProviderVariants = await _db.ProductVariants.AnyAsync(v =>
            v.ProviderCatalogItemID == catalogItem.Id &&
            !v.IsDeleted,
            ct);

        if (hasProviderVariants)
            return;

        await EnsureProviderSizeChartAsync(catalogItem, context.Outfit, ct);

        var baseVariants = await _db.ProductVariants
            .AsNoTracking()
            .Where(v => v.OutfitID == context.Outfit.Id && v.ProviderCatalogItemID == null && !v.IsDeleted)
            .OrderBy(v => v.Size)
            .ToListAsync(ct);

        foreach (var source in baseVariants)
        {
            _db.ProductVariants.Add(new ProductVariant
            {
                Id = Guid.NewGuid(),
                OutfitID = source.OutfitID,
                ProviderCatalogItemID = catalogItem.Id,
                Size = source.Size,
                Price = catalogItem.PublicationPrice,
                StockQuantity = source.StockQuantity,
                ColorVariant = source.ColorVariant,
                MaterialType = source.MaterialType,
                SKUCode = source.SKUCode,
                VariantImageURL = source.VariantImageURL,
                IsDeleted = false
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<ProductVariant?> FindProviderVariantAsync(ProviderCatalogVariantContext context, Guid requestedVariantId, CancellationToken ct)
    {
        var catalogItem = context.CatalogItem ?? throw new InvalidOperationException("Catalog item is required.");
        var variant = await _db.ProductVariants.FirstOrDefaultAsync(v =>
            v.Id == requestedVariantId &&
            v.ProviderCatalogItemID == catalogItem.Id &&
            v.OutfitID == context.Outfit.Id &&
            !v.IsDeleted,
            ct);

        if (variant != null)
            return variant;

        var source = await _db.ProductVariants.AsNoTracking().FirstOrDefaultAsync(v =>
            v.Id == requestedVariantId &&
            v.OutfitID == context.Outfit.Id &&
            v.ProviderCatalogItemID == null &&
            !v.IsDeleted,
            ct);

        if (source == null)
            return null;

        return await _db.ProductVariants.FirstOrDefaultAsync(v =>
            v.ProviderCatalogItemID == catalogItem.Id &&
            v.OutfitID == context.Outfit.Id &&
            v.Size == source.Size &&
            !v.IsDeleted,
            ct);
    }

    private async Task<List<ProductVariantDto>> GetVisibleVariantsAsync(ProviderCatalogVariantContext context, CancellationToken ct)
    {
        if (context.CatalogItem != null)
        {
            var providerVariants = await _db.ProductVariants
                .AsNoTracking()
                .Where(v => v.ProviderCatalogItemID == context.CatalogItem.Id && !v.IsDeleted)
                .OrderBy(v => v.Size)
                .ToListAsync(ct);

            if (providerVariants.Count > 0)
            {
                var providerDetails = await GetSizeDetailsAsync(context.CatalogItem.SizeChartID, ct);
                return MapVariants(providerVariants, providerDetails);
            }
        }

        var baseVariants = await _db.ProductVariants
            .AsNoTracking()
            .Where(v => v.OutfitID == context.Outfit.Id && v.ProviderCatalogItemID == null && !v.IsDeleted)
            .OrderBy(v => v.Size)
            .ToListAsync(ct);
        var baseDetails = await GetSizeDetailsAsync(context.Outfit.SizeChartID, ct);
        return MapVariants(baseVariants, baseDetails);
    }

    private async Task<List<SizeChartDetail>> GetSizeDetailsAsync(Guid? sizeChartId, CancellationToken ct)
    {
        if (!sizeChartId.HasValue)
            return new List<SizeChartDetail>();

        return await _db.SizeChartDetails
            .AsNoTracking()
            .Where(detail => detail.SizeChartID == sizeChartId.Value)
            .Include(detail => detail.Measurements)
            .ToListAsync(ct);
    }

    private static List<ProductVariantDto> MapVariants(IEnumerable<ProductVariant> variants, IReadOnlyCollection<SizeChartDetail> details)
    {
        return variants
            .Select(variant =>
            {
                var detail = details.FirstOrDefault(d => d.SizeLabel == variant.Size);
                return new ProductVariantDto
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
                    Measurements = VariantSizeChartSyncHelper.ToDtos(detail)
                };
            })
            .ToList();
    }

    private async Task EnsureProviderSizeChartAsync(ProviderCatalogItem catalogItem, Outfit outfit, CancellationToken ct)
    {
        if (catalogItem.SizeChartID.HasValue)
            return;

        var chart = new SizeChart
        {
            Id = Guid.NewGuid(),
            ChartName = $"{catalogItem.DisplayName} provider size chart",
            Unit = outfit.SizeChart?.Unit ?? "cm",
        };
        catalogItem.SizeChartID = chart.Id;
        _db.SizeCharts.Add(chart);

        if (!outfit.SizeChartID.HasValue)
            return;

        var sourceDetails = await _db.SizeChartDetails
            .AsNoTracking()
            .Where(d => d.SizeChartID == outfit.SizeChartID.Value)
            .Include(d => d.Measurements)
            .ToListAsync(ct);

        foreach (var sourceDetail in sourceDetails)
        {
            var detail = new SizeChartDetail
            {
                Id = Guid.NewGuid(),
                SizeChartID = chart.Id,
                SizeLabel = sourceDetail.SizeLabel,
            };
            _db.SizeChartDetails.Add(detail);

            foreach (var measurement in sourceDetail.Measurements)
            {
                _db.SizeChartMeasurements.Add(new SizeChartMeasurement
                {
                    Id = Guid.NewGuid(),
                    SizeChartDetailId = detail.Id,
                    FieldKey = measurement.FieldKey,
                    DisplayName = measurement.DisplayName,
                    Unit = measurement.Unit,
                    MinCm = measurement.MinCm,
                    MaxCm = measurement.MaxCm,
                });
            }
        }
    }

    private async Task<SizeChartDetail> EnsureSizeChartDetailAsync(ProviderCatalogItem catalogItem, Outfit outfit, string sizeLabel, CancellationToken ct)
    {
        await EnsureProviderSizeChartAsync(catalogItem, outfit, ct);
        var normalizedLabel = sizeLabel.Trim();
        var detail = await _db.SizeChartDetails
            .Include(d => d.Measurements)
            .FirstOrDefaultAsync(d => d.SizeChartID == catalogItem.SizeChartID && d.SizeLabel == normalizedLabel, ct);

        if (detail != null)
            return detail;

        detail = new SizeChartDetail
        {
            Id = Guid.NewGuid(),
            SizeChartID = catalogItem.SizeChartID!.Value,
            SizeLabel = normalizedLabel,
        };
        _db.SizeChartDetails.Add(detail);
        return detail;
    }

    private async Task<SizeChartDetail> GetOrMoveSizeChartDetailAsync(ProviderCatalogItem catalogItem, Outfit outfit, string originalSize, string activeSize, CancellationToken ct)
    {
        await EnsureProviderSizeChartAsync(catalogItem, outfit, ct);
        var normalizedLabel = activeSize.Trim();
        var existingDetail = await _db.SizeChartDetails
            .Include(d => d.Measurements)
            .FirstOrDefaultAsync(d => d.SizeChartID == catalogItem.SizeChartID && d.SizeLabel == normalizedLabel, ct);

        if (existingDetail != null)
            return existingDetail;

        if (!string.Equals(originalSize, activeSize, StringComparison.OrdinalIgnoreCase))
        {
            var oldDetail = await _db.SizeChartDetails
                .Include(d => d.Measurements)
                .FirstOrDefaultAsync(d => d.SizeChartID == catalogItem.SizeChartID && d.SizeLabel == originalSize, ct);

            if (oldDetail != null)
            {
                oldDetail.SizeLabel = normalizedLabel;
                return oldDetail;
            }
        }

        var detail = new SizeChartDetail
        {
            Id = Guid.NewGuid(),
            SizeChartID = catalogItem.SizeChartID!.Value,
            SizeLabel = normalizedLabel,
        };
        _db.SizeChartDetails.Add(detail);
        return detail;
    }

    private void UpsertMeasurements(Guid sizeChartDetailId, IEnumerable<VariantMeasurementInputDto>? inputs, List<SizeChartMeasurement> existingMeasurements)
    {
        var normalized = VariantSizeChartSyncHelper.NormalizeInputs(inputs);
        var existingByKey = existingMeasurements.ToDictionary(m => m.FieldKey, StringComparer.OrdinalIgnoreCase);

        foreach (var measurement in existingMeasurements)
        {
            if (normalized.All(input => !string.Equals(input.FieldKey, measurement.FieldKey, StringComparison.OrdinalIgnoreCase)))
                _db.SizeChartMeasurements.Remove(measurement);
        }

        foreach (var input in normalized)
        {
            if (existingByKey.TryGetValue(input.FieldKey, out var current))
            {
                current.DisplayName = input.DisplayName;
                current.Unit = string.IsNullOrWhiteSpace(input.Unit) ? "cm" : input.Unit.Trim();
                current.MinCm = input.MinCm;
                current.MaxCm = input.MaxCm;
                continue;
            }

            _db.SizeChartMeasurements.Add(new SizeChartMeasurement
            {
                Id = Guid.NewGuid(),
                SizeChartDetailId = sizeChartDetailId,
                FieldKey = input.FieldKey,
                DisplayName = input.DisplayName,
                Unit = string.IsNullOrWhiteSpace(input.Unit) ? "cm" : input.Unit.Trim(),
                MinCm = input.MinCm,
                MaxCm = input.MaxCm,
            });
        }
    }

    private static string? TrimOrNull(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private record ProviderCatalogVariantContext(
        Guid ProviderId,
        SemesterPublicationProvider PublicationProvider,
        Outfit Outfit,
        ContractItem ContractItem,
        ProviderCatalogItem? CatalogItem);
}

using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;

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

        if (user.SchoolID == null)
            return Result<ProductVariantDto>.Failure("School profile not set up yet.", "SCHOOL_NOT_FOUND");

        // Verify outfit belongs to this school
        var outfit = await _db.Outfits
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == command.OutfitId && !o.IsDeleted, ct);

        if (outfit == null)
            return Result<ProductVariantDto>.Failure("Outfit not found.", "OUTFIT_NOT_FOUND");

        if (outfit.SchoolID != user.SchoolID.Value)
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
        if (command.Size != null) variant.Size = command.Size;
        if (command.Price.HasValue) variant.Price = command.Price.Value;
        if (command.StockQuantity.HasValue) variant.StockQuantity = command.StockQuantity.Value;
        if (command.ColorVariant != null) variant.ColorVariant = command.ColorVariant;
        if (command.MaterialType != null) variant.MaterialType = command.MaterialType;
        if (command.SKUCode != null) variant.SKUCode = command.SKUCode;

        await _db.SaveChangesAsync(ct);

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
        });
    }
}

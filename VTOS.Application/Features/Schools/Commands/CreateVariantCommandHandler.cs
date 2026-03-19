using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Schools.DTOs;
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
            .AsNoTracking()
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
            Size = command.Size,
            Price = outfit.Price, // Inherit price from Outfit
            StockQuantity = 0, // Managed by Provider later
            ColorVariant = command.ColorVariant,
            MaterialType = command.MaterialType,
            SKUCode = command.SKUCode,
            IsDeleted = false,
        };

        _db.ProductVariants.Add(variant);
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

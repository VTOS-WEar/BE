using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Providers.DTOs;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Commands;

public record UpsertProviderCatalogItemCommand(
    Guid UserId,
    Guid SemesterPublicationProviderId,
    Guid OutfitId,
    UpsertProviderCatalogItemRequest Request);

public interface IUpsertProviderCatalogItemCommandHandler
{
    Task<Result<ProviderCatalogItemDto>> HandleAsync(UpsertProviderCatalogItemCommand command, CancellationToken cancellationToken = default);
}

public class UpsertProviderCatalogItemCommandHandler : IUpsertProviderCatalogItemCommandHandler
{
    private readonly IApplicationDbContext _context;

    public UpsertProviderCatalogItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProviderCatalogItemDto>> HandleAsync(UpsertProviderCatalogItemCommand command, CancellationToken cancellationToken = default)
    {
        var providerId = await ResolveProviderIdAsync(command.UserId, cancellationToken);
        if (!providerId.HasValue)
            return Result<ProviderCatalogItemDto>.Failure("Provider not found for current user.", "PROVIDER_NOT_FOUND");

        if (command.Request.PublicationPrice <= 0)
            return Result<ProviderCatalogItemDto>.Failure("Publication price must be greater than 0.", "INVALID_PUBLICATION_PRICE");

        if (command.Request.PostDeadlinePrice <= 0)
            return Result<ProviderCatalogItemDto>.Failure("Post-deadline price must be greater than 0.", "INVALID_POST_DEADLINE_PRICE");

        if (command.Request.PostDeadlinePrice < command.Request.PublicationPrice)
            return Result<ProviderCatalogItemDto>.Failure("Post-deadline price cannot be lower than publication price.", "INVALID_POST_DEADLINE_PRICE");

        if (!Enum.TryParse<ProviderCatalogItemStatus>(command.Request.Status, true, out var status))
            return Result<ProviderCatalogItemDto>.Failure("Invalid catalog item status.", "INVALID_STATUS");

        var publicationProvider = await _context.SemesterPublicationProviders
            .Include(x => x.SemesterPublication)
            .FirstOrDefaultAsync(x =>
                x.Id == command.SemesterPublicationProviderId &&
                x.ProviderID == providerId.Value,
                cancellationToken);

        if (publicationProvider == null)
            return Result<ProviderCatalogItemDto>.Failure("Publication provider assignment not found.", "PUBLICATION_PROVIDER_NOT_FOUND");

        if (publicationProvider.Status == SemPublicationProviderStatus.Suspended)
            return Result<ProviderCatalogItemDto>.Failure("Suspended providers cannot update catalog items for this publication.", "PROVIDER_SUSPENDED");

        if (!publicationProvider.ContractID.HasValue)
            return Result<ProviderCatalogItemDto>.Failure("A linked supplier agreement is required before catalog items can be managed.", "CONTRACT_REQUIRED");

        var publicationOutfit = await _context.SemesterPublicationOutfits
            .Include(x => x.Outfit)
            .FirstOrDefaultAsync(x =>
                x.SemesterPublicationID == publicationProvider.SemesterPublicationID &&
                x.OutfitID == command.OutfitId,
                cancellationToken);

        if (publicationOutfit == null)
            return Result<ProviderCatalogItemDto>.Failure("This outfit is not part of the selected semester publication.", "OUTFIT_NOT_IN_PUBLICATION");

        var contractItem = await _context.ContractItems
            .FirstOrDefaultAsync(x =>
                x.ContractID == publicationProvider.ContractID.Value &&
                x.OutfitID == command.OutfitId,
                cancellationToken);

        if (contractItem == null)
            return Result<ProviderCatalogItemDto>.Failure("The linked supplier agreement does not include this outfit.", "CONTRACT_ITEM_NOT_FOUND");

        var catalogItem = await _context.ProviderCatalogItems
            .FirstOrDefaultAsync(x =>
                x.SemesterPublicationProviderID == publicationProvider.Id &&
                x.ContractItemID == contractItem.Id,
                cancellationToken);

        var now = DateTime.UtcNow;
        if (catalogItem == null)
        {
            catalogItem = new ProviderCatalogItem
            {
                Id = Guid.NewGuid(),
                ProviderID = providerId.Value,
                SemesterPublicationProviderID = publicationProvider.Id,
                ContractItemID = contractItem.Id,
                OutfitID = command.OutfitId,
                CreatedAt = now
            };
            _context.ProviderCatalogItems.Add(catalogItem);
        }

        catalogItem.DisplayName = TrimOrFallback(command.Request.DisplayName, publicationOutfit.Outfit.OutfitName);
        catalogItem.ShortDescription = TrimOrNull(command.Request.ShortDescription);
        catalogItem.MaterialDetails = TrimOrNull(command.Request.MaterialDetails);
        catalogItem.PublicationPrice = command.Request.PublicationPrice;
        catalogItem.PostDeadlinePrice = command.Request.PostDeadlinePrice;
        catalogItem.MainImageUrl = publicationOutfit.Outfit.MainImageURL;
        catalogItem.Status = status;
        catalogItem.UpdatedAt = now;

        if (status == ProviderCatalogItemStatus.Published && catalogItem.PublishedAt == null)
            catalogItem.PublishedAt = now;
        if (status == ProviderCatalogItemStatus.Hidden)
            catalogItem.HiddenAt = now;
        if (status != ProviderCatalogItemStatus.Hidden)
            catalogItem.HiddenAt = null;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<ProviderCatalogItemDto>.Success(new ProviderCatalogItemDto
        {
            CatalogItemId = catalogItem.Id,
            ContractItemId = contractItem.Id,
            OutfitId = publicationOutfit.OutfitID,
            OutfitName = publicationOutfit.Outfit.OutfitName,
            OutfitImageUrl = publicationOutfit.Outfit.MainImageURL,
            SchoolMaterialType = publicationOutfit.Outfit.MaterialType,
            ContractPricePerUnit = contractItem.PricePerUnit,
            DisplayName = catalogItem.DisplayName,
            ShortDescription = catalogItem.ShortDescription,
            MaterialDetails = catalogItem.MaterialDetails,
            PublicationPrice = catalogItem.PublicationPrice,
            PostDeadlinePrice = catalogItem.PostDeadlinePrice,
            Status = catalogItem.Status.ToString()
        });
    }

    private async Task<Guid?> ResolveProviderIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.ProviderManagers
            .AsNoTracking()
            .Where(x => x.UserID == userId)
            .Select(x => (Guid?)x.ProviderID)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string TrimOrFallback(string? value, string fallback)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? fallback : trimmed;
    }

    private static string? TrimOrNull(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}

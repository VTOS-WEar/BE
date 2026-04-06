using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>School confirms a specific delivery record with accepted/defective quantities.</summary>
public record ConfirmDeliveryCommand(Guid UserId, Guid BatchId, Guid DeliveryRecordId, int AcceptedQuantity, int? DefectiveQuantity, string? DefectNote);

public interface IConfirmDeliveryCommandHandler
{
    Task<Result<string>> HandleAsync(ConfirmDeliveryCommand command, CancellationToken ct = default);
}

public class ConfirmDeliveryCommandHandler : IConfirmDeliveryCommandHandler
{
    private readonly IApplicationDbContext _db;

    public ConfirmDeliveryCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<string>> HandleAsync(ConfirmDeliveryCommand command, CancellationToken ct = default)
    {
        // Resolve school
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        var schoolMgr = await _db.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<string>.Failure("School not found.", "SCHOOL_NOT_FOUND");

        // Get batch via campaign's school
        var batch = await _db.ProductionBatches
            .Include(b => b.Campaign)
            .Include(b => b.DeliveryRecords)
            .FirstOrDefaultAsync(b => b.Id == command.BatchId
                && b.Campaign.SchoolID == schoolMgr.SchoolID
                && !b.IsDeleted, ct);

        if (batch == null)
            return Result<string>.Failure("Production order not found.", "BATCH_NOT_FOUND");

        // Find the specific delivery record
        var delivery = batch.DeliveryRecords.FirstOrDefault(dr => dr.Id == command.DeliveryRecordId);
        if (delivery == null)
            return Result<string>.Failure("Delivery record not found.", "DELIVERY_NOT_FOUND");

        if (delivery.IsConfirmed)
            return Result<string>.Failure("This delivery has already been confirmed.", "ALREADY_CONFIRMED");

        // Validate quantities
        if (command.AcceptedQuantity < 0)
            return Result<string>.Failure("Accepted quantity cannot be negative.", "INVALID_QUANTITY");

        var defective = command.DefectiveQuantity ?? 0;
        if (command.AcceptedQuantity + defective > delivery.Quantity)
            return Result<string>.Failure(
                $"Accepted ({command.AcceptedQuantity}) + defective ({defective}) exceeds delivered ({delivery.Quantity}).", "EXCEEDS_DELIVERED");

        // Update delivery record
        delivery.IsConfirmed = true;
        delivery.ConfirmedAt = DateTime.UtcNow;
        delivery.AcceptedQuantity = command.AcceptedQuantity;
        delivery.DefectiveQuantity = defective;
        delivery.DefectNote = command.DefectNote;
        delivery.UpdatedAt = DateTime.UtcNow;

        // If all deliveries are confirmed, mark batch delivery as confirmed
        var allConfirmed = batch.DeliveryRecords.All(dr => dr.IsConfirmed);
        if (allConfirmed)
        {
            batch.DeliveryConfirmedAt = DateTime.UtcNow;

            // Auto-fulfill contract: check if ALL batches for this campaign+provider are delivery-confirmed
            var allBatchesForProvider = await _db.ProductionBatches
                .Where(b => b.CampaignID == batch.CampaignID
                          && b.ProviderID == batch.ProviderID
                          && !b.IsDeleted)
                .ToListAsync(ct);

            var allBatchesConfirmed = allBatchesForProvider.All(b =>
                b.Id == batch.Id ? true : b.DeliveryConfirmedAt != null);

            if (allBatchesConfirmed)
            {
                // Find the InUse contract linked to this campaign+provider
                var campaignOutfit = await _db.CampaignOutfits.AsNoTracking()
                    .FirstOrDefaultAsync(co => co.CampaignID == batch.CampaignID
                                            && co.ProviderID == batch.ProviderID
                                            && co.ContractID != null, ct);

                if (campaignOutfit?.ContractID != null)
                {
                    var contract = await _db.Contracts
                        .FirstOrDefaultAsync(c => c.Id == campaignOutfit.ContractID && c.Status == "InUse", ct);
                    if (contract != null)
                    {
                        contract.Status = "Fulfilled";
                    }
                }
            }
        }

        // Auto-create complaint if defective > 0
        if (defective > 0 && !string.IsNullOrEmpty(command.DefectNote))
        {
            var complaint = new Domain.Entities.Complaint
            {
                Id = Guid.NewGuid(),
                CampaignID = batch.CampaignID,
                BatchID = batch.Id,
                SchoolID = schoolMgr.SchoolID,
                ProviderID = batch.ProviderID,
                Title = $"Defective uniforms in delivery ({defective} items)",
                Description = command.DefectNote,
                Status = Domain.Enums.ComplaintStatus.Open,
                CreatedAt = DateTime.UtcNow
            };
            _db.Complaints.Add(complaint);
        }

        await _db.SaveChangesAsync(ct);

        return Result<string>.Success(allConfirmed
            ? "Delivery confirmed. All deliveries are now confirmed."
            : "Delivery confirmed successfully.");
    }
}

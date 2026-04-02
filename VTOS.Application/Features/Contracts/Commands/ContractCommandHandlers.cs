using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Contracts.DTOs;
using VTOS.Application.Features.Notifications;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Contracts.Commands;

// ─── Shared mapping helper ───
internal static class ContractMapper
{
    internal static ContractDto MapToDto(Contract c) => new(
        c.Id, c.SchoolID, c.ProviderID,
        c.ContractName, c.Status, c.CreatedAt,
        c.ApprovedAt, c.RejectedAt, c.RejectionReason,
        c.School?.SchoolName, c.Provider?.ProviderName,
        c.ContractItems.Select(ci => new ContractItemDto(
            ci.Id, ci.OutfitID, ci.Outfit?.OutfitName ?? "", ci.PricePerUnit, ci.MinQuantity, ci.MaxQuantity
        )).ToList()
    );

    internal static IQueryable<Contract> IncludeAll(IQueryable<Contract> q) =>
        q.Include(c => c.School)
         .Include(c => c.Provider)
         .Include(c => c.ContractItems).ThenInclude(ci => ci.Outfit);
}

// ─── Create Contract Handler (School) ───
public class CreateContractCommandHandler : ICreateContractCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public CreateContractCommandHandler(IApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<Result<ContractDto>> HandleAsync(
        CreateContractCommand command, CancellationToken ct = default)
    {
        // Resolve SchoolID from UserId
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        var schoolMgr = await _context.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        var providerMgr = await _context.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<ContractDto>.Failure("User is not linked to a school.", "NOT_SCHOOL");

        var schoolId = schoolMgr.SchoolID;
        var req = command.Request;

        // Validate provider exists
        var provider = await _context.Providers.FindAsync(new object[] { req.ProviderId }, ct);
        if (provider == null)
            return Result<ContractDto>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        // Validate all outfits belong to this school
        var outfitIds = req.Items.Select(i => i.OutfitId).ToList();
        var schoolOutfits = await _context.Outfits
            .Where(o => outfitIds.Contains(o.Id) && o.SchoolID == schoolId && !o.IsDeleted)
            .Select(o => o.Id)
            .ToListAsync(ct);

        var missingOutfits = outfitIds.Except(schoolOutfits).ToList();
        if (missingOutfits.Any())
            return Result<ContractDto>.Failure(
                $"Outfits not found or don't belong to your school: {string.Join(", ", missingOutfits)}",
                "INVALID_OUTFITS");

        // Validate quantity ranges
        foreach (var item in req.Items)
        {
            if (item.MinQuantity < 0 || item.MaxQuantity < item.MinQuantity)
                return Result<ContractDto>.Failure(
                    $"Invalid quantity range for outfit {item.OutfitId}: min={item.MinQuantity}, max={item.MaxQuantity}",
                    "INVALID_QUANTITY");
            if (item.PricePerUnit <= 0)
                return Result<ContractDto>.Failure(
                    $"Price per unit must be positive for outfit {item.OutfitId}",
                    "INVALID_PRICE");
        }

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            SchoolID = schoolId,
            ProviderID = req.ProviderId,
            ContractName = req.ContractName,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
        };

        foreach (var item in req.Items)
        {
            contract.ContractItems.Add(new ContractItem
            {
                Id = Guid.NewGuid(),
                ContractID = contract.Id,
                OutfitID = item.OutfitId,
                PricePerUnit = item.PricePerUnit,
                MinQuantity = item.MinQuantity,
                MaxQuantity = item.MaxQuantity,
            });
        }

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync(ct);

        // Notify provider about new contract
        try
        {
            var school = await _context.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == schoolId, ct);
            await _notificationService.NotifyProviderAsync(req.ProviderId,
                "📋 Hợp đồng mới",
                $"{school?.SchoolName ?? "Trường"} gửi hợp đồng: {contract.ContractName}.",
                "Contract", contract.Id, "Contract",
                "/provider/contracts", ct);
        }
        catch { /* Don't fail the main operation */ }

        // Reload with navigation for response
        var saved = await ContractMapper.IncludeAll(_context.Contracts.AsQueryable())
            .FirstOrDefaultAsync(x => x.Id == contract.Id, ct);
        return Result<ContractDto>.Success(ContractMapper.MapToDto(saved!));
    }
}

// ─── Approve Contract Handler (Provider) ───
public class ApproveContractCommandHandler : IApproveContractCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    public ApproveContractCommandHandler(IApplicationDbContext context, IEmailService emailService, INotificationService notificationService)
    {
        _context = context;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    public async Task<Result<ContractDto>> HandleAsync(
        ApproveContractCommand command, CancellationToken ct = default)
    {
        // Resolve ProviderID from UserId
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null)
            return Result<ContractDto>.Failure("User not found.", "NOT_FOUND");
        var providerMgr = await _context.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (providerMgr == null)
            return Result<ContractDto>.Failure("User is not linked to a provider.", "NOT_PROVIDER");

        var providerId = providerMgr.ProviderID;

        var contract = await ContractMapper.IncludeAll(_context.Contracts.AsQueryable())
            .FirstOrDefaultAsync(c => c.Id == command.ContractId && c.ProviderID == providerId, ct);

        if (contract == null)
            return Result<ContractDto>.Failure("Contract not found.", "NOT_FOUND");

        if (contract.Status != "Pending")
            return Result<ContractDto>.Failure(
                $"Contract is '{contract.Status}', only Pending contracts can be approved.",
                "INVALID_STATUS");

        contract.Status = "Approved";
        contract.ApprovedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        // Notify school (in-app + SignalR)
        try
        {
            await _notificationService.NotifySchoolAsync(contract.SchoolID,
                "✅ Hợp đồng đã duyệt",
                $"NCC {contract.Provider?.ProviderName} đã duyệt hợp đồng: {contract.ContractName}.",
                "Contract", contract.Id, "Contract",
                "/school/contracts", ct);
        }
        catch { /* Don't fail */ }

        // Send email notification to the School about contract approval
        try
        {
            var schoolMgr = await _context.SchoolManagers.AsNoTracking()
                .FirstOrDefaultAsync(m => m.SchoolID == contract.SchoolID, ct);
            if (schoolMgr != null)
            {
                var schoolUser = await _context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == schoolMgr.UserID, ct);
                if (schoolUser != null)
                {
                    await _emailService.SendContractReplyNotificationAsync(
                        schoolUser.Email, schoolUser.FullName,
                        contract.ContractName, "Approved", user.FullName, ct);
                }
            }
        }
        catch { /* Email failure should not block the response */ }

        return Result<ContractDto>.Success(ContractMapper.MapToDto(contract));
    }
}

// ─── Reject Contract Handler (Provider) ───
public class RejectContractCommandHandler : IRejectContractCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    public RejectContractCommandHandler(IApplicationDbContext context, IEmailService emailService, INotificationService notificationService)
    {
        _context = context;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    public async Task<Result<ContractDto>> HandleAsync(
        RejectContractCommand command, CancellationToken ct = default)
    {
        // Resolve ProviderID from UserId
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null)
            return Result<ContractDto>.Failure("User not found.", "NOT_FOUND");
        var providerMgr = await _context.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (providerMgr == null)
            return Result<ContractDto>.Failure("User is not linked to a provider.", "NOT_PROVIDER");

        var providerId = providerMgr.ProviderID;

        var contract = await ContractMapper.IncludeAll(_context.Contracts.AsQueryable())
            .FirstOrDefaultAsync(c => c.Id == command.ContractId && c.ProviderID == providerId, ct);

        if (contract == null)
            return Result<ContractDto>.Failure("Contract not found.", "NOT_FOUND");

        if (contract.Status != "Pending")
            return Result<ContractDto>.Failure(
                $"Contract is '{contract.Status}', only Pending contracts can be rejected.",
                "INVALID_STATUS");

        contract.Status = "Rejected";
        contract.RejectedAt = DateTime.UtcNow;
        contract.RejectionReason = command.Reason;
        await _context.SaveChangesAsync(ct);

        // Notify school (in-app + SignalR)
        try
        {
            await _notificationService.NotifySchoolAsync(contract.SchoolID,
                "❌ Hợp đồng bị từ chối",
                $"NCC {contract.Provider?.ProviderName} đã từ chối hợp đồng: {contract.ContractName}.",
                "Contract", contract.Id, "Contract",
                "/school/contracts", ct);
        }
        catch { /* Don't fail */ }

        // Send email notification to the School about contract rejection
        try
        {
            var schoolMgr = await _context.SchoolManagers.AsNoTracking()
                .FirstOrDefaultAsync(m => m.SchoolID == contract.SchoolID, ct);
            if (schoolMgr != null)
            {
                var schoolUser = await _context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == schoolMgr.UserID, ct);
                if (schoolUser != null)
                {
                    await _emailService.SendContractReplyNotificationAsync(
                        schoolUser.Email, schoolUser.FullName,
                        contract.ContractName, "Rejected", user.FullName, ct);
                }
            }
        }
        catch { /* Email failure should not block the response */ }

        return Result<ContractDto>.Success(ContractMapper.MapToDto(contract));
    }
}

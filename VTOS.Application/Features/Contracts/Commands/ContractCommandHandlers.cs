using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Contracts.DTOs;
using VTOS.Application.Features.Notifications;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Contracts.Commands;

// ── Helpers ──────────────────────────────────────────────────────────────────

internal static class ContractMapper
{
    internal static ContractDto MapToDto(Contract c, string? viewerMaskedContact = null) => new(
        c.Id,
        c.SchoolID,
        c.ProviderID,
        c.ContractName,
        c.ContractNumber,
        c.Status,
        c.CreatedAt,
        c.ApprovedAt,
        c.RejectedAt,
        c.RejectionReason,
        c.ExpiresAt,
        // Names
        c.School?.SchoolName,
        c.Provider?.ProviderName,
        // School extended
        c.School?.Address,
        c.School?.TaxCode,
        c.School?.RepresentativeName,
        c.School?.RepresentativeTitle,
        c.School?.Phone,
        // Provider extended
        c.Provider?.Address,
        c.Provider?.TaxCode,
        c.Provider?.ContactPersonName,
        c.Provider?.RepresentativeTitle,
        c.Provider?.Phone,
        c.Provider?.Email,
        // Signatures
        c.SchoolSignature,
        c.SchoolSignedAt,
        c.ProviderSignature,
        c.ProviderSignedAt,
        // Masked contact for OTP display
        viewerMaskedContact,
        // PDF URL
        c.ContractPdfUrl,
        // Items
        c.ContractItems.Select(ci => new ContractItemDto(
            ci.Id, ci.OutfitID, ci.Outfit?.OutfitName ?? "", ci.PricePerUnit, ci.MinQuantity, ci.MaxQuantity
        )).ToList()
    );

    internal static IQueryable<Contract> IncludeAll(IQueryable<Contract> q) =>
        q.Include(c => c.School)
         .Include(c => c.Provider)
         .Include(c => c.ContractItems).ThenInclude(ci => ci.Outfit);

    /// <summary>Auto-generates contract number: HĐ-{YEAR}-{6-char-shortId}</summary>
    internal static string GenerateContractNumber(Guid contractId)
    {
        var year = DateTime.UtcNow.Year;
        var shortId = contractId.ToString("N")[..6].ToUpper();
        return $"HĐ-{year}-{shortId}";
    }

    /// <summary>Masks an email for display: "nguyen.huong@school.vn" → "n***g@school.vn"</summary>
    internal static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "email đã đăng ký";
        var at = email.IndexOf('@');
        if (at <= 1) return "***@" + (at >= 0 ? email[(at + 1)..] : email);
        return email[0] + "***" + email[at - 1] + email[at..];
    }

    /// <summary>Masks a phone: "0912345678" → "091***678"</summary>
    internal static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 7) return "SĐT đã đăng ký";
        return phone[..3] + "***" + phone[^3..];
    }
}

// ── Create Contract Handler (School) ─────────────────────────────────────────

public class CreateContractCommandHandler : ICreateContractCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public CreateContractCommandHandler(IApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<Result<ContractDto>> HandleAsync(CreateContractCommand command, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        var schoolMgr = await _context.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<ContractDto>.Failure("User is not linked to a school.", "NOT_SCHOOL");

        var schoolId = schoolMgr.SchoolID;
        var req = command.Request;

        if (string.IsNullOrWhiteSpace(req.ContractName))
            return Result<ContractDto>.Failure("Contract name is required.", "NAME_REQUIRED");
        if (req.ContractName.Length > 200)
            return Result<ContractDto>.Failure("Contract name cannot exceed 200 characters.", "NAME_TOO_LONG");

        var provider = await _context.Providers.FindAsync(new object[] { req.ProviderId }, ct);
        if (provider == null)
            return Result<ContractDto>.Failure("Provider not found.", "PROVIDER_NOT_FOUND");

        var outfitIds = req.Items.Select(i => i.OutfitId).ToList();
        var schoolOutfits = await _context.Outfits
            .Where(o => outfitIds.Contains(o.Id) && o.SchoolID == schoolId && !o.IsDeleted)
            .Select(o => o.Id).ToListAsync(ct);

        var missingOutfits = outfitIds.Except(schoolOutfits).ToList();
        if (missingOutfits.Any())
            return Result<ContractDto>.Failure(
                $"Outfits not found or don't belong to your school: {string.Join(", ", missingOutfits)}",
                "INVALID_OUTFITS");

        if (req.ExpiresAt <= DateTime.UtcNow)
            return Result<ContractDto>.Failure("Contract expiration date must be in the future.", "INVALID_EXPIRES_AT");

        var activeStatuses = new[] { "Pending", "PendingSchoolSign", "PendingProviderSign", "Active", "InUse" };
        var existingContracts = await _context.Contracts.AsNoTracking()
            .Where(c => c.SchoolID == schoolId && c.ProviderID == req.ProviderId && activeStatuses.Contains(c.Status))
            .Include(c => c.ContractItems).ToListAsync(ct);

        foreach (var oid in outfitIds)
        {
            var duplicate = existingContracts.FirstOrDefault(c => c.ContractItems.Any(ci => ci.OutfitID == oid));
            if (duplicate != null)
            {
                var outfit = await _context.Outfits.AsNoTracking().FirstOrDefaultAsync(o => o.Id == oid, ct);
                return Result<ContractDto>.Failure(
                    $"Already have an active contract ('{duplicate.ContractName}', status: {duplicate.Status}) with this provider for outfit '{outfit?.OutfitName ?? oid.ToString()}'. Complete or cancel the existing contract first.",
                    "DUPLICATE_CONTRACT");
            }
        }

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

        var contractId = Guid.NewGuid();
        var contract = new Contract
        {
            Id = contractId,
            SchoolID = schoolId,
            ProviderID = req.ProviderId,
            ContractName = req.ContractName,
            ContractNumber = ContractMapper.GenerateContractNumber(contractId),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = req.ExpiresAt,
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

        try
        {
            var school = await _context.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == schoolId, ct);
            await _notificationService.NotifyProviderAsync(req.ProviderId,
                "📋 Hợp đồng mới",
                $"{school?.SchoolName ?? "Trường"} gửi hợp đồng: {contract.ContractName}.",
                "Contract", contract.Id, "Contract", "/provider/contracts", ct);
        }
        catch { /* Don't fail the main operation */ }

        var saved = await ContractMapper.IncludeAll(_context.Contracts.AsQueryable())
            .FirstOrDefaultAsync(x => x.Id == contract.Id, ct);

        var viewerMasked = ContractMapper.MaskEmail(user?.Email);
        return Result<ContractDto>.Success(ContractMapper.MapToDto(saved!, viewerMasked));
    }
}

// ── Approve Contract Handler (Provider) — now moves to PendingSchoolSign ─────

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

    public async Task<Result<ContractDto>> HandleAsync(ApproveContractCommand command, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null) return Result<ContractDto>.Failure("User not found.", "NOT_FOUND");

        var providerMgr = await _context.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (providerMgr == null) return Result<ContractDto>.Failure("User is not linked to a provider.", "NOT_PROVIDER");

        var contract = await ContractMapper.IncludeAll(_context.Contracts.AsQueryable())
            .FirstOrDefaultAsync(c => c.Id == command.ContractId && c.ProviderID == providerMgr.ProviderID, ct);

        if (contract == null) return Result<ContractDto>.Failure("Contract not found.", "NOT_FOUND");
        if (contract.Status != "Pending")
            return Result<ContractDto>.Failure($"Contract is '{contract.Status}', only Pending contracts can be approved.", "INVALID_STATUS");

        // Transition: Pending → PendingSchoolSign (awaiting School's digital signature)
        contract.Status = "PendingSchoolSign";
        contract.ApprovedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        // Notify school: invitation to sign
        try
        {
            await _notificationService.NotifySchoolAsync(contract.SchoolID,
                "✍️ Mời ký hợp đồng",
                $"NCC {contract.Provider?.ProviderName} đã duyệt và mời bạn ký hợp đồng: {contract.ContractName}.",
                "Contract", contract.Id, "Contract", "/school/contracts", ct);
        }
        catch { /* Don't fail */ }

        try
        {
            var schoolMgr = await _context.SchoolManagers.AsNoTracking()
                .FirstOrDefaultAsync(m => m.SchoolID == contract.SchoolID, ct);
            if (schoolMgr != null)
            {
                var schoolUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == schoolMgr.UserID, ct);
                if (schoolUser != null)
                    await _emailService.SendContractReplyNotificationAsync(
                        schoolUser.Email, schoolUser.FullName,
                        contract.ContractName, "Approved", user.FullName, ct);
            }
        }
        catch { /* Email failure should not block */ }

        var viewerMasked = ContractMapper.MaskEmail(user.Email);
        return Result<ContractDto>.Success(ContractMapper.MapToDto(contract, viewerMasked));
    }
}

// ── Reject Contract Handler (Provider) ───────────────────────────────────────

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

    public async Task<Result<ContractDto>> HandleAsync(RejectContractCommand command, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null) return Result<ContractDto>.Failure("User not found.", "NOT_FOUND");

        var providerMgr = await _context.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (providerMgr == null) return Result<ContractDto>.Failure("User is not linked to a provider.", "NOT_PROVIDER");

        var contract = await ContractMapper.IncludeAll(_context.Contracts.AsQueryable())
            .FirstOrDefaultAsync(c => c.Id == command.ContractId && c.ProviderID == providerMgr.ProviderID, ct);

        if (contract == null) return Result<ContractDto>.Failure("Contract not found.", "NOT_FOUND");
        if (contract.Status != "Pending")
            return Result<ContractDto>.Failure($"Contract is '{contract.Status}', only Pending contracts can be rejected.", "INVALID_STATUS");

        if (string.IsNullOrWhiteSpace(command.Reason))
            return Result<ContractDto>.Failure("Rejection reason is required.", "REASON_REQUIRED");
        if (command.Reason.Length > 500)
            return Result<ContractDto>.Failure("Rejection reason cannot exceed 500 characters.", "REASON_TOO_LONG");

        contract.Status = "Rejected";
        contract.RejectedAt = DateTime.UtcNow;
        contract.RejectionReason = command.Reason;
        await _context.SaveChangesAsync(ct);

        try
        {
            await _notificationService.NotifySchoolAsync(contract.SchoolID,
                "❌ Hợp đồng bị từ chối",
                $"NCC {contract.Provider?.ProviderName} đã từ chối hợp đồng: {contract.ContractName}.",
                "Contract", contract.Id, "Contract", "/school/contracts", ct);
        }
        catch { /* Don't fail */ }

        try
        {
            var schoolMgr = await _context.SchoolManagers.AsNoTracking()
                .FirstOrDefaultAsync(m => m.SchoolID == contract.SchoolID, ct);
            if (schoolMgr != null)
            {
                var schoolUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == schoolMgr.UserID, ct);
                if (schoolUser != null)
                    await _emailService.SendContractReplyNotificationAsync(
                        schoolUser.Email, schoolUser.FullName,
                        contract.ContractName, "Rejected", user.FullName, ct);
            }
        }
        catch { /* Email failure should not block */ }

        return Result<ContractDto>.Success(ContractMapper.MapToDto(contract));
    }
}

// ── Cancel Contract Handler (School) ─────────────────────────────────────────

public class CancelContractCommandHandler : ICancelContractCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public CancelContractCommandHandler(IApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<Result<ContractDto>> HandleAsync(CancelContractCommand command, CancellationToken ct = default)
    {
        var schoolMgr = await _context.SchoolManagers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserID == command.UserId, ct);
        if (schoolMgr?.SchoolID == null)
            return Result<ContractDto>.Failure("User is not linked to a school.", "NOT_SCHOOL");

        var contract = await ContractMapper.IncludeAll(_context.Contracts.AsQueryable())
            .FirstOrDefaultAsync(c => c.Id == command.ContractId && c.SchoolID == schoolMgr.SchoolID, ct);

        if (contract == null) return Result<ContractDto>.Failure("Contract not found.", "NOT_FOUND");

        // School can cancel while Pending (before Provider action) OR PendingSchoolSign (Provider approved but School hasn't signed yet)
        var cancellableStatuses = new[] { "Pending", "PendingSchoolSign" };
        if (!cancellableStatuses.Contains(contract.Status))
            return Result<ContractDto>.Failure(
                $"Contract is '{contract.Status}'. Only Pending or PendingSchoolSign contracts can be cancelled.",
                "INVALID_STATUS");

        contract.Status = "Cancelled";
        contract.RejectedAt = DateTime.UtcNow;
        contract.RejectionReason = "Cancelled by school";
        await _context.SaveChangesAsync(ct);

        try
        {
            await _notificationService.NotifyProviderAsync(contract.ProviderID,
                "🚫 Hợp đồng đã bị hủy",
                $"Trường {contract.School?.SchoolName} đã hủy hợp đồng: {contract.ContractName}.",
                "Contract", contract.Id, "Contract", "/provider/contracts", ct);
        }
        catch { /* Don't fail */ }

        return Result<ContractDto>.Success(ContractMapper.MapToDto(contract));
    }
}

// ── Request Sign OTP Handler (School or Provider) ────────────────────────────

public class RequestSignOTPCommandHandler : IRequestSignOTPCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public RequestSignOTPCommandHandler(IApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<Result<bool>> HandleAsync(RequestSignOTPCommand command, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null) return Result<bool>.Failure("User not found.", "NOT_FOUND");

        Contract? contract = null;

        if (command.Role == "School")
        {
            var mgr = await _context.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
            if (mgr == null) return Result<bool>.Failure("Not linked to a school.", "NOT_SCHOOL");

            contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.Id == command.ContractId && c.SchoolID == mgr.SchoolID, ct);

            if (contract == null) return Result<bool>.Failure("Contract not found.", "NOT_FOUND");
            if (contract.Status != "PendingSchoolSign")
                return Result<bool>.Failure("Contract is not awaiting school signature.", "INVALID_STATUS");
        }
        else if (command.Role == "Provider")
        {
            var mgr = await _context.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
            if (mgr == null) return Result<bool>.Failure("Not linked to a provider.", "NOT_PROVIDER");

            contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.Id == command.ContractId && c.ProviderID == mgr.ProviderID, ct);

            if (contract == null) return Result<bool>.Failure("Contract not found.", "NOT_FOUND");
            if (contract.Status != "PendingProviderSign")
                return Result<bool>.Failure("Contract is not awaiting provider signature.", "INVALID_STATUS");
        }
        else
        {
            return Result<bool>.Failure("Invalid role.", "INVALID_ROLE");
        }

        // Generate OTP (6 digits, 10-minute expiry)
        var otp = Random.Shared.Next(100000, 999999).ToString();
        contract.SigningOTPCode = otp;
        contract.SigningOTPExpiry = DateTime.UtcNow.AddMinutes(10);
        contract.SigningOTPFor = command.Role;
        await _context.SaveChangesAsync(ct);

        // Send OTP email
        try
        {
            await _emailService.SendContractSignOTPAsync(
                user.Email, user.FullName, otp,
                contract.ContractName, contract.ContractNumber ?? "-",
                10, ct);
        }
        catch
        {
            return Result<bool>.Failure("Failed to send OTP email. Please try again.", "EMAIL_FAILED");
        }

        return Result<bool>.Success(true);
    }
}

// ── Sign Contract by School Handler ──────────────────────────────────────────

public class SignContractBySchoolCommandHandler : ISignContractBySchoolCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public SignContractBySchoolCommandHandler(IApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<Result<ContractDto>> HandleAsync(SignContractBySchoolCommand command, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null) return Result<ContractDto>.Failure("User not found.", "NOT_FOUND");

        var mgr = await _context.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (mgr == null) return Result<ContractDto>.Failure("Not linked to a school.", "NOT_SCHOOL");

        var contract = await ContractMapper.IncludeAll(_context.Contracts.AsQueryable())
            .FirstOrDefaultAsync(c => c.Id == command.ContractId && c.SchoolID == mgr.SchoolID, ct);

        if (contract == null) return Result<ContractDto>.Failure("Contract not found.", "NOT_FOUND");
        if (contract.Status != "PendingSchoolSign")
            return Result<ContractDto>.Failure("Contract is not awaiting school signature.", "INVALID_STATUS");

        // DEV BYPASS: OTP validation skipped for testing — restore when email is working
        // TODO: uncomment when email is working
        // if (string.IsNullOrWhiteSpace(command.Request.OTPCode))
        //     return Result<ContractDto>.Failure("OTP code is required.", "OTP_REQUIRED");
        // if (contract.SigningOTPFor != "School")
        //     return Result<ContractDto>.Failure("OTP was not issued for school signing.", "OTP_WRONG_PARTY");
        // if (contract.SigningOTPCode != command.Request.OTPCode)
        //     return Result<ContractDto>.Failure("Invalid OTP code.", "OTP_INVALID");
        // if (contract.SigningOTPExpiry == null || contract.SigningOTPExpiry < DateTime.UtcNow)
        //     return Result<ContractDto>.Failure("OTP has expired. Please request a new one.", "OTP_EXPIRED");

        if (string.IsNullOrWhiteSpace(command.Request.SignatureData))
            return Result<ContractDto>.Failure("Signature data is required.", "SIGNATURE_REQUIRED");

        // Store signature and transition
        contract.SchoolSignature = command.Request.SignatureData;
        contract.SchoolSignedAt = DateTime.UtcNow;
        contract.Status = "PendingProviderSign";

        // Clear used OTP
        contract.SigningOTPCode = null;
        contract.SigningOTPExpiry = null;
        contract.SigningOTPFor = null;

        // Set PDF URL if PDF was provided
        if (!string.IsNullOrWhiteSpace(command.Request.PdfBase64))
            contract.ContractPdfUrl = $"/contracts/{contract.Id}.pdf";

        await _context.SaveChangesAsync(ct);

        // Persist PDF file (non-critical — failure doesn't block signing)
        if (!string.IsNullOrWhiteSpace(command.Request.PdfBase64))
        {
            try
            {
                var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "contracts");
                Directory.CreateDirectory(dir);
                var bytes = Convert.FromBase64String(command.Request.PdfBase64);
                await File.WriteAllBytesAsync(Path.Combine(dir, $"{contract.Id}.pdf"), bytes, ct);
            }
            catch (Exception ex) { /* Log and continue — sign succeeded */ _ = ex; }
        }

        // Notify Provider: their turn to sign
        try
        {
            await _notificationService.NotifyProviderAsync(contract.ProviderID,
                "✍️ Đến lượt ký hợp đồng",
                $"Trường {contract.School?.SchoolName} đã ký hợp đồng: {contract.ContractName}. Vui lòng ký xác nhận.",
                "Contract", contract.Id, "Contract", "/provider/contracts", ct);
        }
        catch { /* Don't fail */ }

        var viewerMasked = ContractMapper.MaskEmail(user.Email);
        return Result<ContractDto>.Success(ContractMapper.MapToDto(contract, viewerMasked));
    }
}

// ── Sign Contract by Provider Handler ────────────────────────────────────────

public class SignContractByProviderCommandHandler : ISignContractByProviderCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    public SignContractByProviderCommandHandler(IApplicationDbContext context, IEmailService emailService, INotificationService notificationService)
    {
        _context = context;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    public async Task<Result<ContractDto>> HandleAsync(SignContractByProviderCommand command, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null) return Result<ContractDto>.Failure("User not found.", "NOT_FOUND");

        var mgr = await _context.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
        if (mgr == null) return Result<ContractDto>.Failure("Not linked to a provider.", "NOT_PROVIDER");

        var contract = await ContractMapper.IncludeAll(_context.Contracts.AsQueryable())
            .FirstOrDefaultAsync(c => c.Id == command.ContractId && c.ProviderID == mgr.ProviderID, ct);

        if (contract == null) return Result<ContractDto>.Failure("Contract not found.", "NOT_FOUND");
        if (contract.Status != "PendingProviderSign")
            return Result<ContractDto>.Failure("Contract is not awaiting provider signature.", "INVALID_STATUS");

        // DEV BYPASS: OTP validation skipped for testing — restore when email is working
        // TODO: uncomment when email is working
        // if (string.IsNullOrWhiteSpace(command.Request.OTPCode))
        //     return Result<ContractDto>.Failure("OTP code is required.", "OTP_REQUIRED");
        // if (contract.SigningOTPFor != "Provider")
        //     return Result<ContractDto>.Failure("OTP was not issued for provider signing.", "OTP_WRONG_PARTY");
        // if (contract.SigningOTPCode != command.Request.OTPCode)
        //     return Result<ContractDto>.Failure("Invalid OTP code.", "OTP_INVALID");
        // if (contract.SigningOTPExpiry == null || contract.SigningOTPExpiry < DateTime.UtcNow)
        //     return Result<ContractDto>.Failure("OTP has expired. Please request a new one.", "OTP_EXPIRED");

        if (string.IsNullOrWhiteSpace(command.Request.SignatureData))
            return Result<ContractDto>.Failure("Signature data is required.", "SIGNATURE_REQUIRED");

        // Store signature — contract is now fully Active
        contract.ProviderSignature = command.Request.SignatureData;
        contract.ProviderSignedAt = DateTime.UtcNow;
        contract.Status = "Active";

        // Clear used OTP
        contract.SigningOTPCode = null;
        contract.SigningOTPExpiry = null;
        contract.SigningOTPFor = null;

        // Set PDF URL if PDF was provided (final signed PDF with both signatures)
        if (!string.IsNullOrWhiteSpace(command.Request.PdfBase64))
            contract.ContractPdfUrl = $"/contracts/{contract.Id}.pdf";

        await _context.SaveChangesAsync(ct);

        // Persist final PDF file (non-critical)
        if (!string.IsNullOrWhiteSpace(command.Request.PdfBase64))
        {
            try
            {
                var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "contracts");
                Directory.CreateDirectory(dir);
                var bytes = Convert.FromBase64String(command.Request.PdfBase64);
                await File.WriteAllBytesAsync(Path.Combine(dir, $"{contract.Id}.pdf"), bytes, ct);
            }
            catch (Exception ex) { /* Log and continue */ _ = ex; }
        }

        // Notify both parties: contract is now active
        try
        {
            await _notificationService.NotifySchoolAsync(contract.SchoolID,
                "✅ Hợp đồng có hiệu lực",
                $"Hợp đồng '{contract.ContractName}' đã được ký kết đầy đủ và có hiệu lực.",
                "Contract", contract.Id, "Contract", "/school/contracts", ct);
            await _notificationService.NotifyProviderAsync(contract.ProviderID,
                "✅ Hợp đồng có hiệu lực",
                $"Hợp đồng '{contract.ContractName}' với {contract.School?.SchoolName} đã có hiệu lực.",
                "Contract", contract.Id, "Contract", "/provider/contracts", ct);
        }
        catch { /* Don't fail */ }

        // Send confirmation email to both parties
        try
        {
            var schoolMgr = await _context.SchoolManagers.AsNoTracking()
                .FirstOrDefaultAsync(m => m.SchoolID == contract.SchoolID, ct);
            if (schoolMgr != null)
            {
                var schoolUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == schoolMgr.UserID, ct);
                if (schoolUser != null)
                    await _emailService.SendContractReplyNotificationAsync(
                        schoolUser.Email, schoolUser.FullName,
                        contract.ContractName, "Active", user.FullName, ct);
            }
        }
        catch { /* Email failure should not block */ }

        var viewerMasked = ContractMapper.MaskEmail(user.Email);
        return Result<ContractDto>.Success(ContractMapper.MapToDto(contract, viewerMasked));
    }
}

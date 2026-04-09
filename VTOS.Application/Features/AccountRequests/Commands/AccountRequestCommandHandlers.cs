using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.AccountRequests.DTOs;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;
using VTOS.Application.Features.Notifications;

namespace VTOS.Application.Features.AccountRequests.Commands;

// ─── Shared mapping helper ───
internal static class AccountRequestMapper
{
    internal static AccountRequestDetailDto MapToDetailDto(AccountRequest r) => new(
        r.Id,
        r.OrganizationName,
        r.ContactEmail,
        r.ContactPhone,
        r.ContactPersonName,
        r.Type.ToString(),
        r.Description,
        r.Address,
        r.Status.ToString(),
        r.RejectionReason,
        r.ProcessedByUserId,
        r.ProcessedByUser?.FullName,
        r.CreatedUserId,
        r.CreatedAt,
        r.ProcessedAt
    );

    internal static AccountRequestListItemDto MapToListDto(AccountRequest r) => new(
        r.Id,
        r.OrganizationName,
        r.ContactEmail,
        r.ContactPhone,
        r.ContactPersonName,
        r.Type.ToString(),
        r.Status.ToString(),
        r.CreatedAt,
        r.ProcessedAt
    );
}

// ─── Submit Account Request Handler (Public) ───
public class SubmitAccountRequestCommandHandler : ISubmitAccountRequestCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public SubmitAccountRequestCommandHandler(
        IApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<Result<AccountRequestDetailDto>> HandleAsync(
        SubmitAccountRequestCommand command, CancellationToken ct = default)
    {
        var req = command.Request;

        // Validate required fields
        if (string.IsNullOrWhiteSpace(req.OrganizationName))
            return Result<AccountRequestDetailDto>.Failure("Tên tổ chức không được để trống.", "VALIDATION");
        if (req.OrganizationName.Length > 200)
            return Result<AccountRequestDetailDto>.Failure("Tên tổ chức không được vượt quá 200 ký tự.", "ORG_NAME_TOO_LONG");
        if (string.IsNullOrWhiteSpace(req.ContactEmail))
            return Result<AccountRequestDetailDto>.Failure("Email không được để trống.", "VALIDATION");
        if (string.IsNullOrWhiteSpace(req.ContactPhone))
            return Result<AccountRequestDetailDto>.Failure("Số điện thoại không được để trống.", "VALIDATION");
        if (req.Type != 1 && req.Type != 2)
            return Result<AccountRequestDetailDto>.Failure("Loại tài khoản không hợp lệ.", "VALIDATION");
        if (req.Description != null && req.Description.Length > 1000)
            return Result<AccountRequestDetailDto>.Failure("Mô tả không được vượt quá 1000 ký tự.", "DESCRIPTION_TOO_LONG");
        if (req.Address != null && req.Address.Length > 500)
            return Result<AccountRequestDetailDto>.Failure("Địa chỉ không được vượt quá 500 ký tự.", "ADDRESS_TOO_LONG");

        // Check duplicate pending requests by email
        var existingRequest = await _context.AccountRequests
            .AnyAsync(ar => ar.ContactEmail == req.ContactEmail
                         && ar.Status == AccountRequestStatus.Pending, ct);

        if (existingRequest)
            return Result<AccountRequestDetailDto>.Failure(
                "Đã có yêu cầu đang chờ xử lý với email này.", "DUPLICATE_REQUEST");

        var accountRequest = new AccountRequest
        {
            Id = Guid.NewGuid(),
            OrganizationName = req.OrganizationName.Trim(),
            ContactEmail = req.ContactEmail.Trim().ToLower(),
            ContactPhone = req.ContactPhone.Trim(),
            Type = (AccountRequestType)req.Type,
            Description = req.Description?.Trim(),
            Address = req.Address?.Trim(),
            ContactPersonName = req.ContactPersonName?.Trim(),
            Status = AccountRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };

        _context.AccountRequests.Add(accountRequest);
        await _context.SaveChangesAsync(ct);

        // Notify all admins
        var typeLabel = accountRequest.Type == AccountRequestType.School ? "Trường học" : "Nhà cung cấp";
        try
        {
            await _notificationService.NotifyAdminsAsync(
                "📋 Yêu cầu hợp tác mới",
                $"{accountRequest.OrganizationName} ({typeLabel}) gửi yêu cầu hợp tác.",
                "AccountRequest",
                accountRequest.Id, "AccountRequest",
                "/admin/account-requests", ct);
        }
        catch { /* Don't fail the main operation */ }

        return Result<AccountRequestDetailDto>.Success(AccountRequestMapper.MapToDetailDto(accountRequest));
    }
}

// ─── Create Account For Request Handler (Admin) ───
public class CreateAccountForRequestCommandHandler : ICreateAccountForRequestCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    public CreateAccountForRequestCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        INotificationService notificationService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    public async Task<Result<AccountRequestDetailDto>> HandleAsync(
        CreateAccountForRequestCommand command, CancellationToken ct = default)
    {
        var accountRequest = await _context.AccountRequests
            .FirstOrDefaultAsync(ar => ar.Id == command.RequestId, ct);

        if (accountRequest == null)
            return Result<AccountRequestDetailDto>.Failure("Yêu cầu không tồn tại.", "NOT_FOUND");

        if (accountRequest.Status != AccountRequestStatus.Pending)
            return Result<AccountRequestDetailDto>.Failure(
                $"Yêu cầu đã được xử lý ({accountRequest.Status}).", "ALREADY_PROCESSED");

        var req = command.Request;
        var email = req.Email.Trim().ToLower();

        // Check if email already exists
        var emailExists = await _context.Users.AnyAsync(u => u.Email == email, ct);
        if (emailExists)
            return Result<AccountRequestDetailDto>.Failure(
                $"Email '{email}' đã tồn tại trong hệ thống.", "EMAIL_EXISTS");

        // Determine role
        var roleName = accountRequest.Type == AccountRequestType.School ? "School" : "Provider";
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName, ct);
        if (role == null)
        {
            role = new Role
            {
                Id = Guid.NewGuid(),
                RoleName = roleName,
                Description = $"{roleName} user role",
                IsSystemRole = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Roles.Add(role);
        }

        // Generate temporary password
        var tempPassword = GenerateTempPassword();

        // Create User
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(tempPassword),
            FullName = req.FullName?.Trim() ?? accountRequest.ContactPersonName ?? "User",
            Phone = req.Phone?.Trim(),
            RoleID = role.Id,
            IsActive = true,  // Active immediately (no OTP needed — admin-created)
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);

        // Create role-specific entity
        if (accountRequest.Type == AccountRequestType.School)
        {
            var school = new School
            {
                Id = Guid.NewGuid(),
                SchoolName = accountRequest.OrganizationName,
                ContactInfo = "{}",
                Level = "",
            };
            _context.Schools.Add(school);

            _context.SchoolManagers.Add(new SchoolManager
            {
                Id = Guid.NewGuid(),
                UserID = user.Id,
                SchoolID = school.Id
            });
        }
        else // Provider
        {
            var provider = new Provider
            {
                Id = Guid.NewGuid(),
                ProviderName = accountRequest.OrganizationName,
                Email = email,
                Status = ProviderStatus.Active,
                IsDeleted = false
            };
            _context.Providers.Add(provider);

            _context.ProviderManagers.Add(new ProviderManager
            {
                Id = Guid.NewGuid(),
                UserID = user.Id,
                ProviderID = provider.Id
            });
        }

        // Update request status
        accountRequest.Status = AccountRequestStatus.Approved;
        accountRequest.ProcessedByUserId = command.AdminUserId;
        accountRequest.CreatedUserId = user.Id;
        accountRequest.ProcessedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        // Send credentials email
        try
        {
            await _emailService.SendAccountCredentialsEmailAsync(
                email, tempPassword, roleName, ct);
        }
        catch (Exception)
        {
            // Log error but don't fail — account is already created
        }

        // Send welcome in-app notification
        try
        {
            await _notificationService.CreateAsync(user.Id,
                "🎉 Chào mừng bạn!",
                $"Tài khoản {roleName} của bạn đã được tạo thành công. Hãy cập nhật thông tin hồ sơ.",
                "Welcome", null, null,
                roleName == "School" ? "/school/profile" : "/provider/profile", ct);
        }
        catch { /* Don't fail */ }

        // Reload with navigation
        var saved = await _context.AccountRequests
            .Include(ar => ar.ProcessedByUser)
            .Include(ar => ar.CreatedUser)
            .FirstOrDefaultAsync(ar => ar.Id == accountRequest.Id, ct);

        return Result<AccountRequestDetailDto>.Success(AccountRequestMapper.MapToDetailDto(saved!));
    }

    private static string GenerateTempPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

// ─── Reject Account Request Handler (Admin) ───
public class RejectAccountRequestCommandHandler : IRejectAccountRequestCommandHandler
{
    private readonly IApplicationDbContext _context;

    public RejectAccountRequestCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<AccountRequestDetailDto>> HandleAsync(
        RejectAccountRequestCommand command, CancellationToken ct = default)
    {
        var accountRequest = await _context.AccountRequests
            .FirstOrDefaultAsync(ar => ar.Id == command.RequestId, ct);

        if (accountRequest == null)
            return Result<AccountRequestDetailDto>.Failure("Yêu cầu không tồn tại.", "NOT_FOUND");

        if (accountRequest.Status != AccountRequestStatus.Pending)
            return Result<AccountRequestDetailDto>.Failure(
                $"Yêu cầu đã được xử lý ({accountRequest.Status}).", "ALREADY_PROCESSED");

        accountRequest.Status = AccountRequestStatus.Rejected;
        if (command.Reason != null && command.Reason.Length > 500)
            return Result<AccountRequestDetailDto>.Failure("Lý do từ chối không được vượt quá 500 ký tự.", "REASON_TOO_LONG");
        accountRequest.RejectionReason = command.Reason;
        accountRequest.ProcessedByUserId = command.AdminUserId;
        accountRequest.ProcessedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return Result<AccountRequestDetailDto>.Success(AccountRequestMapper.MapToDetailDto(accountRequest));
    }
}

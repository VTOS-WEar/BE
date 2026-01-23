using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Auth.DTOs;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Auth.Commands;

/// <summary>
/// Handler for user registration command.
/// Creates INACTIVE user and sends OTP email for verification.
/// </summary>
public class RegisterCommandHandler : IRegisterCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;

    public RegisterCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IEmailService emailService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
    }

    public async Task<Result<RegisterResponse>> HandleAsync(
        RegisterCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if email already exists
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == command.Email, cancellationToken);

        if (emailExists)
        {
            return Result<RegisterResponse>.Failure(
                "Email already exists",
                "EMAIL_EXISTS");
        }

        // Get default "Parent" role
        var parentRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleName == "Parent", cancellationToken);

        if (parentRole == null)
        {
            // Create Parent role if not exists (first-time setup)
            parentRole = new Role
            {
                Id = Guid.NewGuid(),
                RoleName = "Parent",
                Description = "Parent user role",
                IsSystemRole = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Roles.Add(parentRole);
        }

        // Create new user (INACTIVE until email verified)
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            PasswordHash = _passwordHasher.HashPassword(command.Password),
            FullName = command.FullName,
            Phone = null, // Phone collected after first login
            RoleID = parentRole.Id,
            IsActive = false, // INACTIVE until email verified
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        // Generate OTP and create verification record
        var otpCode = OTPGenerator.Generate();
        var verification = new EmailVerification
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            OTPCode = otpCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailVerifications.Add(verification);
        await _context.SaveChangesAsync(cancellationToken);

        // Send OTP email
        try
        {
            await _emailService.SendOTPEmailAsync(command.Email, otpCode, cancellationToken);
        }
        catch (Exception)
        {
            // Log error but don't fail registration
            // User can request resend OTP
        }

        return Result<RegisterResponse>.Success(new RegisterResponse(
            user.Id,
            user.Email,
            user.FullName,
            "Registration successful. Please check your email for verification code."
        ));
    }
}

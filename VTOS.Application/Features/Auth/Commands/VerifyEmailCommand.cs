using VTOS.Application.Common;

namespace VTOS.Application.Features.Auth.Commands;

/// <summary>
/// Command for verifying email with OTP code.
/// </summary>
public record VerifyEmailCommand(
    string Email,
    string OTPCode
);

/// <summary>
/// Handler interface for VerifyEmailCommand.
/// </summary>
public interface IVerifyEmailCommandHandler
{
    Task<Result<string>> HandleAsync(VerifyEmailCommand command, CancellationToken cancellationToken = default);
}

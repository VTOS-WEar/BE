namespace VTOS.Application.Abstractions;

/// <summary>
/// Verifies Cloudflare Turnstile tokens before sensitive public actions.
/// </summary>
public interface ITurnstileVerifier
{
    Task<TurnstileVerificationResult> VerifyAsync(string token, CancellationToken cancellationToken = default);
}

public record TurnstileVerificationResult(
    bool Success,
    IReadOnlyList<string> ErrorCodes
);

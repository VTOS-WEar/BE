namespace VTOS.Infrastructure.ExternalServices.Turnstile;

/// <summary>
/// Cloudflare Turnstile server-side validation settings.
/// </summary>
public class TurnstileSettings
{
    public const string SectionName = "Turnstile";

    public string SecretKey { get; set; } = string.Empty;
    public string? ExpectedHostname { get; set; }
    public string SiteVerifyUrl { get; set; } = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
}

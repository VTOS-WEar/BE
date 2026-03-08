namespace VTOS.Application.Common.Settings;

/// <summary>
/// Payment service configuration settings
/// </summary>
public class PaymentSettings
{
    public const string SectionName = "PaymentSettings";

    /// <summary>
    /// Base URL for payment return redirects (e.g., https://yourdomain.com)
    /// </summary>
    public string ReturnBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Path for payment success return (appended to ReturnBaseUrl)
    /// </summary>
    public string ReturnSuccessPath { get; set; } = "/payment/return";

    /// <summary>
    /// Path for payment cancellation return (appended to ReturnBaseUrl)
    /// </summary>
    public string ReturnCancelPath { get; set; } = "/payment/cancel";
}

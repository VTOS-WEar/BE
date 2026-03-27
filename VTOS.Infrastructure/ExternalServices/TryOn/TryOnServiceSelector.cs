using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VTOS.Application.Abstractions;

namespace VTOS.Infrastructure.ExternalServices.TryOn;

/// <summary>
/// Settings for try-on provider selection
/// </summary>
public class TryOnProviderSettings
{
    public const string SectionName = "TryOnProvider";
    
    /// <summary>
    /// Which provider to use: "302ai", "gemini", or "auto" (try Gemini first, fallback to 302.ai)
    /// </summary>
    public string Provider { get; set; } = "302ai";
}

/// <summary>
/// Strategy selector that delegates to the appropriate try-on service
/// based on configuration. When set to "auto", tries Gemini first then falls back to 302.ai.
/// </summary>
public class TryOnServiceSelector : IVirtualTryOnService
{
    private readonly VirtualTryOnService _service302;
    private readonly GeminiTryOnService _geminiService;
    private readonly TryOnProviderSettings _providerSettings;
    private readonly ILogger<TryOnServiceSelector> _logger;

    public TryOnServiceSelector(
        VirtualTryOnService service302,
        GeminiTryOnService geminiService,
        IOptions<TryOnProviderSettings> providerSettings,
        ILogger<TryOnServiceSelector> logger)
    {
        _service302 = service302;
        _geminiService = geminiService;
        _providerSettings = providerSettings.Value;
        _logger = logger;
    }

    public async Task<TryOnResult> ProcessAsync(
        string humanImageUrl,
        string garmentImageUrl,
        CancellationToken cancellationToken = default)
    {
        var provider = _providerSettings.Provider?.ToLowerInvariant() ?? "302ai";

        switch (provider)
        {
            case "gemini":
                _logger.LogInformation("Using Gemini try-on service");
                return await _geminiService.ProcessAsync(humanImageUrl, garmentImageUrl, cancellationToken);

            case "auto":
                _logger.LogInformation("Auto mode: trying Gemini first, fallback to 302.ai");
                var geminiResult = await _geminiService.ProcessAsync(humanImageUrl, garmentImageUrl, cancellationToken);
                if (geminiResult.Success)
                    return geminiResult;

                _logger.LogWarning("Gemini failed ({Error}), falling back to 302.ai", geminiResult.Error);
                return await _service302.ProcessAsync(humanImageUrl, garmentImageUrl, cancellationToken);

            case "302ai":
            default:
                _logger.LogInformation("Using 302.ai try-on service");
                return await _service302.ProcessAsync(humanImageUrl, garmentImageUrl, cancellationToken);
        }
    }
}

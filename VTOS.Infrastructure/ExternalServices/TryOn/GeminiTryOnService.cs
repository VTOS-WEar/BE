using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VTOS.Application.Abstractions;

namespace VTOS.Infrastructure.ExternalServices.TryOn;

/// <summary>
/// Settings for Gemini-based Virtual Try-On (Nano Banana 2 model)
/// </summary>
public class GeminiTryOnSettings
{
    public const string SectionName = "GeminiTryOn";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-3.1-flash-image-preview";
    
    /// <summary>
    /// Custom prompt for try-on generation.
    /// Can be overridden via appsettings or environment variable GeminiTryOn__Prompt.
    /// </summary>
    public string Prompt { get; set; } = @"Using the two provided images, take the uniform from the first image and make the person in the second image wear it.

Change only the clothing on the person in the second image.
Keep everything else in the second image exactly the same.

Strictly preserve the exact body shape, body size, and visible person scale in frame from the second image:
same shoulder width, chest width, waist position, hip width, torso length, arm thickness, hand size, leg proportions, neck length, and the same distance to the camera.

Do not make the person slimmer, wider, taller, shorter, larger in frame, smaller in frame, closer to the camera, or farther from the camera.

Preserve the exact pose, face, identity, hairstyle, hands, skin tone, crop, framing, zoom, perspective, camera angle, aspect ratio, background, surrounding objects, lighting, shadows, and original color mood from the second image.

Only replace the outfit with the uniform from the first image.
Adapt the uniform naturally to the existing body shape of the person in the second image without changing the outer body silhouette, except for minimal natural fabric thickness and folds.

Keep the uniform details accurate:
garment cut, collar shape, neckline, sleeve shape, cuff shape, hemline, seams, trim, stripe placement, logo placement, badge placement, embroidery, text, pattern, and fabric texture.

Keep the background and scene colors unchanged.
The final result should be photorealistic, sharp, clean, and high-detail, with crisp garment edges, realistic fabric folds, and preserved fine details.
Do not change the input aspect ratio.";
}

/// <summary>
/// Gemini API-based virtual try-on using image generation (Nano Banana 2 approach).
/// Downloads images, sends as base64 inline data, gets generated result.
/// </summary>
public class GeminiTryOnService : IVirtualTryOnService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiTryOnSettings _settings;
    private readonly ILogger<GeminiTryOnService> _logger;

    public GeminiTryOnService(
        HttpClient httpClient,
        IOptions<GeminiTryOnSettings> settings,
        ILogger<GeminiTryOnService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<TryOnResult> ProcessAsync(
        string humanImageUrl,
        string garmentImageUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Gemini Try-On: Processing. Human: {HumanUrl}, Garment: {GarmentUrl}",
                humanImageUrl, garmentImageUrl);

            // Step 1: Download both images as base64
            var humanBytes = await _httpClient.GetByteArrayAsync(humanImageUrl, cancellationToken);
            var garmentBytes = await _httpClient.GetByteArrayAsync(garmentImageUrl, cancellationToken);

            var humanBase64 = Convert.ToBase64String(humanBytes);
            var garmentBase64 = Convert.ToBase64String(garmentBytes);

            // Step 2: Build Gemini API request
            var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = _settings.Prompt },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "image/jpeg",
                                    data = garmentBase64
                                }
                            },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "image/jpeg",
                                    data = humanBase64
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    responseModalities = new[] { "TEXT", "IMAGE" }
                }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(apiUrl, jsonContent, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("Gemini API response status: {Status}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseContent);
                return new TryOnResult(false, null, $"Gemini API error: {response.StatusCode}");
            }

            // Step 3: Parse response — extract generated image
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Length == 0)
            {
                _logger.LogWarning("Gemini returned no candidates");
                return new TryOnResult(false, null, "Gemini returned no results");
            }

            // Find inline image in response parts
            foreach (var candidate in geminiResponse.Candidates)
            {
                if (candidate.Content?.Parts == null) continue;
                foreach (var part in candidate.Content.Parts)
                {
                    if (part.InlineData != null && part.InlineData.MimeType?.StartsWith("image/") == true)
                    {
                        // Convert base64 back to data URL for frontend display
                        var dataUrl = $"data:{part.InlineData.MimeType};base64,{part.InlineData.Data}";
                        _logger.LogInformation("Gemini Try-On successful. Generated image size: {Size} bytes",
                            part.InlineData.Data?.Length ?? 0);
                        return new TryOnResult(true, dataUrl, null);
                    }
                }
            }

            _logger.LogWarning("Gemini response contained no image parts");
            return new TryOnResult(false, null, "Gemini did not generate an image");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Gemini try-on processing");
            return new TryOnResult(false, null, $"Gemini service error: {ex.Message}");
        }
    }

    #region Gemini Response Models
    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public GeminiCandidate[]? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }

    private class GeminiContent
    {
        [JsonPropertyName("parts")]
        public GeminiPart[]? Parts { get; set; }
    }

    private class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("inlineData")]
        public GeminiInlineData? InlineData { get; set; }
    }

    private class GeminiInlineData
    {
        [JsonPropertyName("mimeType")]
        public string? MimeType { get; set; }

        [JsonPropertyName("data")]
        public string? Data { get; set; }
    }
    #endregion
}

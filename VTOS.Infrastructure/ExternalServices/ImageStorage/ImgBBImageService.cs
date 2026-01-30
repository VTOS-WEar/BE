using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VTOS.Application.Abstractions;

namespace VTOS.Infrastructure.ExternalServices.ImageStorage;

/// <summary>
/// Implementation of image upload service using ImgBB API
/// </summary>
public class ImgBBImageService : IImageUploadService
{
    private readonly HttpClient _httpClient;
    private readonly ImgBBSettings _settings;
    private readonly ILogger<ImgBBImageService> _logger;

    public ImgBBImageService(
        HttpClient httpClient,
        IOptions<ImgBBSettings> settings,
        ILogger<ImgBBImageService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Stream imageStream, 
        string fileName, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Uploading image to ImgBB: {FileName}", fileName);

            // Convert stream to base64
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream, cancellationToken);
            var base64Image = Convert.ToBase64String(memoryStream.ToArray());

            // Build form data
            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(_settings.ApiKey), "key");
            formData.Add(new StringContent(base64Image), "image");
            formData.Add(new StringContent(Path.GetFileNameWithoutExtension(fileName)), "name");
            
            if (_settings.Expiration > 0)
            {
                formData.Add(new StringContent(_settings.Expiration.ToString()), "expiration");
            }

            var response = await _httpClient.PostAsync(_settings.ApiUrl, formData, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("ImgBB response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("ImgBB API returned error. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseContent);
                throw new InvalidOperationException($"Image upload failed: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<ImgBBResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Success != true || string.IsNullOrEmpty(result.Data?.Url))
            {
                _logger.LogError("ImgBB upload failed. Response: {Response}", responseContent);
                throw new InvalidOperationException("Image upload failed: Invalid response");
            }

            _logger.LogInformation("Image uploaded successfully. URL: {Url}", result.Data.Url);
            return result.Data.Url;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error uploading image to ImgBB");
            throw new InvalidOperationException($"Image upload failed: {ex.Message}", ex);
        }
    }

    private class ImgBBResponse
    {
        public bool Success { get; set; }
        public ImgBBData? Data { get; set; }
    }

    private class ImgBBData
    {
        public string? Url { get; set; }
        public string? DisplayUrl { get; set; }
        public string? DeleteUrl { get; set; }
    }
}

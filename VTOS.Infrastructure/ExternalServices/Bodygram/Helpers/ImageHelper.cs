using System.Drawing;
using Microsoft.AspNetCore.Http;
namespace VTOS.Infrastructure.Bodygram.Helpers;

/// <summary>
/// Helper class for image validation and conversion operations
/// </summary>
public static class ImageHelper
{
    // Image validation constants
    private const long MaxFileSizeBytes = 3 * 1024 * 1024; // 3 MB
    private const long MaxFileSizeMB = 3;
    private static readonly string[] AllowedFormats = { "jpeg", "jpg" };
    private static readonly (int width, int height)[] AllowedResolutions = { (1080, 1920), (720, 1280) };

    /// <summary>
    /// Validates photo format, size, and resolution according to Bodygram requirements
    /// </summary>
    /// <param name="file">Image file to validate</param>
    /// <param name="fieldName">Name of the field being validated (for error messages)</param>
    /// <returns>Validation error message, or null if validation passes</returns>
    public static string? ValidatePhotoFile(IFormFile file, string fieldName = "Photo")
    {
        if (file == null)
            return $"{fieldName} is required";

        if (file.Length == 0)
            return $"{fieldName} is empty";

        // Validate file format
        var formatError = ValidateImageFormat(file);
        if (formatError != null)
            return $"{fieldName}: {formatError}";

        // Validate file size
        var sizeError = ValidateImageFileSize(file);
        if (sizeError != null)
            return $"{fieldName}: {sizeError}";

        // Validate image resolution
        var resolutionError = ValidateImageResolution(file);
        if (resolutionError != null)
            return $"{fieldName}: {resolutionError}";

        return null;
    }

    /// <summary>
    /// Validates that the file format is JPEG
    /// </summary>
    /// <param name="file">Image file to validate</param>
    /// <returns>Error message if invalid, null if valid</returns>
    private static string? ValidateImageFormat(IFormFile file)
    {
        var contentType = file.ContentType?.ToLower() ?? string.Empty;
        var extension = Path.GetExtension(file.FileName).ToLower().TrimStart('.');

        // Check content type
        if (!contentType.Contains("jpeg") && !contentType.Contains("jpg"))
            return $"Invalid format. Only JPEG files are allowed (received: {contentType})";

        // Check file extension
        if (!AllowedFormats.Contains(extension))
            return $"Invalid format. Only .jpg or .jpeg extensions are allowed (received: .{extension})";

        return null;
    }

    /// <summary>
    /// Validates that the file size does not exceed 3 MB
    /// </summary>
    /// <param name="file">Image file to validate</param>
    /// <returns>Error message if invalid, null if valid</returns>
    private static string? ValidateImageFileSize(IFormFile file)
    {
        if (file.Length > MaxFileSizeBytes)
        {
            var fileSizeMB = Math.Round(file.Length / (1024.0 * 1024.0), 2);
            return $"File size exceeds limit. Maximum {MaxFileSizeMB} MB allowed (received: {fileSizeMB} MB)";
        }

        return null;
    }

    /// <summary>
    /// Validates that the image resolution is either 1080x1920 or 720x1280
    /// </summary>
    /// <param name="file">Image file to validate</param>
    /// <returns>Error message if invalid, null if valid</returns>
    private static string? ValidateImageResolution(IFormFile file)
    {
        try
        {
            using (var stream = file.OpenReadStream())
            {
                using (var image = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: false))
                {
                    int width = image.Width;
                    int height = image.Height;

                    // Check if resolution matches allowed dimensions
                    var isValidResolution = AllowedResolutions.Any(res =>
                        (res.width == width && res.height == height) ||
                        (res.height == width && res.width == height) // Allow rotated images
                    );

                    if (!isValidResolution)
                    {
                        var validResolutions = string.Join(" or ", AllowedResolutions.Select(r => $"{r.width}×{r.height}"));
                        return $"Invalid resolution. Required: {validResolutions} (received: {width}×{height})";
                    }

                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            return $"Unable to read image dimensions: {ex.Message}";
        }
    }
    /// <summary>
    /// Converts an image stream to base64 encoded string
    /// </summary>
    /// <param name="imageStream">Stream containing the image data</param>
    /// <returns>Base64 encoded string of the image</returns>
    public static string ConvertImageToBase64(Stream imageStream)
    {
        if (imageStream == null)
            throw new ArgumentNullException(nameof(imageStream));

        using (var memoryStream = new MemoryStream())
        {
            imageStream.CopyTo(memoryStream);
            byte[] imageBytes = memoryStream.ToArray();
            return Convert.ToBase64String(imageBytes);
        }
    }

    /// <summary>
    /// Converts an image file to base64 encoded string
    /// </summary>
    /// <param name="filePath">Path to the image file</param>
    /// <returns>Base64 encoded string of the image</returns>
    public static string ConvertImageFileToBase64(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Image file not found: {filePath}");

        byte[] imageBytes = File.ReadAllBytes(filePath);
        return Convert.ToBase64String(imageBytes);
    }

    /// <summary>
    /// Converts a base64 encoded image string to a stream
    /// </summary>
    /// <param name="base64String">Base64 encoded image string</param>
    /// <returns>Stream containing the image data</returns>
    public static MemoryStream ConvertBase64ToImageStream(string base64String)
    {
        if (string.IsNullOrWhiteSpace(base64String))
            throw new ArgumentNullException(nameof(base64String));

        byte[] imageBytes = Convert.FromBase64String(base64String);
        return new MemoryStream(imageBytes);
    }

    /// <summary>
    /// Converts a base64 encoded avatar (OBJ model) to a byte array
    /// </summary>
    /// <param name="base64Avatar">Base64 encoded avatar data</param>
    /// <returns>Byte array of the avatar</returns>
    public static byte[] ConvertBase64AvatarToBytes(string base64Avatar)
    {
        if (string.IsNullOrWhiteSpace(base64Avatar))
            throw new ArgumentNullException(nameof(base64Avatar));

        return Convert.FromBase64String(base64Avatar);
    }

    /// <summary>
    /// Saves a base64 encoded avatar to a file
    /// </summary>
    /// <param name="base64Avatar">Base64 encoded avatar data</param>
    /// <param name="outputPath">Path where the avatar file should be saved</param>
    public static async Task SaveBase64AvatarToFileAsync(string base64Avatar, string outputPath)
    {
        if (string.IsNullOrWhiteSpace(base64Avatar))
            throw new ArgumentNullException(nameof(base64Avatar));

        byte[] avatarBytes = Convert.FromBase64String(base64Avatar);
        await File.WriteAllBytesAsync(outputPath, avatarBytes);
    }
}

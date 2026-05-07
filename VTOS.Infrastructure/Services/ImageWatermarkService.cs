using SkiaSharp;
using VTOS.Application.Abstractions;

namespace VTOS.Infrastructure.Services;

public class ImageWatermarkService : IImageWatermarkService
{
    private const string WatermarkText = "VTOS PREVIEW";
    private const int OutputQuality = 90;

    public WatermarkedImage ApplyTryOnGuestWatermark(byte[] imageBytes)
    {
        using var source = SKBitmap.Decode(imageBytes)
            ?? throw new InvalidOperationException("Unable to decode try-on image for watermarking.");

        var imageInfo = new SKImageInfo(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;

        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(source, 0, 0);
        DrawWatermark(canvas, source.Width, source.Height);
        canvas.Flush();

        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, OutputQuality);
        return new WatermarkedImage(encoded.ToArray(), "image/jpeg");
    }

    private static void DrawWatermark(SKCanvas canvas, int width, int height)
    {
        var minDimension = Math.Min(width, height);
        var fontSize = Math.Clamp(minDimension / 14f, 28f, 72f);

        using var typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
        using var font = new SKFont(typeface, fontSize)
        {
            Edging = SKFontEdging.Antialias,
            Subpixel = true
        };
        using var strokePaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(15, 23, 42, 110),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Clamp(fontSize / 14f, 2f, 5f)
        };
        using var fillPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(255, 255, 255, 170),
            Style = SKPaintStyle.Fill
        };

        var textWidth = font.MeasureText(WatermarkText);
        var diagonal = MathF.Sqrt(width * width + height * height);
        var stepX = textWidth + fontSize * 2.5f;
        var stepY = fontSize * 3.2f;

        canvas.Save();
        canvas.RotateDegrees(-32, width / 2f, height / 2f);

        for (var y = -diagonal; y < diagonal * 1.5f; y += stepY)
        {
            for (var x = -diagonal; x < diagonal * 1.5f; x += stepX)
            {
                canvas.DrawText(WatermarkText, x, y, font, strokePaint);
                canvas.DrawText(WatermarkText, x, y, font, fillPaint);
            }
        }

        canvas.Restore();
    }
}

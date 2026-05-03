namespace VTOS.Application.Abstractions;

public record WatermarkedImage(
    byte[] Bytes,
    string ContentType);

public interface IImageWatermarkService
{
    WatermarkedImage ApplyTryOnGuestWatermark(byte[] imageBytes);
}

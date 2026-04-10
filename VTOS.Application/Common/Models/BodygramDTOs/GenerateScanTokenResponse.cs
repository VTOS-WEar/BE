namespace VTOS.Application.Common.Models.BodygramDTOs;

public class GenerateScanTokenResponse
{
    public string Token { get; set; } = string.Empty;
    public long ExpiresAt { get; set; }
}

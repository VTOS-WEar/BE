namespace VTOS.Application.Common.Settings;

public class TryOnImageSecuritySettings
{
    public const string SectionName = "TryOnImageSecurity";

    public string SigningKey { get; set; } = string.Empty;
    public int TicketLifetimeMinutes { get; set; } = 5;
    public int StorageReadUrlSeconds { get; set; } = 60;
}

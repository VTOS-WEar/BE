namespace VTOS.Infrastructure.Services;

/// <summary>
/// Email service configuration settings.
/// </summary>
public class EmailSettings
{
    public const string SectionName = "EmailSettings";
    
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "VTOS System";
    public bool EnableSsl { get; set; } = true;
}

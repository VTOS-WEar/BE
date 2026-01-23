using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using VTOS.Application.Abstractions;

namespace VTOS.Infrastructure.Services;

/// <summary>
/// Email service implementation using MailKit.
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task SendOTPEmailAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "VTOS - Email Verification Code";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>Email Verification</h2>
                        <p>Thank you for registering with VTOS (Virtual Try-On System).</p>
                        <p>Your verification code is:</p>
                        <div style='background-color: #f4f4f4; padding: 15px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 5px; margin: 20px 0;'>
                            {otpCode}
                        </div>
                        <p>This code will expire in <strong>10 minutes</strong>.</p>
                        <p>If you didn't request this code, please ignore this email.</p>
                        <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                        <p style='color: #888; font-size: 12px;'>VTOS - Virtual Try-On System</p>
                    </div>
                </body>
                </html>
            ",
            TextBody = $@"
                Email Verification
                
                Thank you for registering with VTOS (Virtual Try-On System).
                
                Your verification code is: {otpCode}
                
                This code will expire in 10 minutes.
                
                If you didn't request this code, please ignore this email.
                
                VTOS - Virtual Try-On System
            "
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, 
            _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, 
            cancellationToken);
        
        await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}

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

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "VTOS - Password Reset Request";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>Password Reset Request</h2>
                        <p>We received a request to reset your password for your VTOS account.</p>
                        <p>Click the button below to reset your password:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{resetLink}' style='background-color: #4CAF50; color: white; padding: 14px 28px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Reset Password</a>
                        </div>
                        <p>Or copy and paste this link into your browser:</p>
                        <p style='word-break: break-all; color: #666;'>{resetLink}</p>
                        <p>This link will expire in <strong>1 hour</strong>.</p>
                        <p style='color: #e74c3c;'>If you didn't request a password reset, please ignore this email or contact support if you have concerns.</p>
                        <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                        <p style='color: #888; font-size: 12px;'>VTOS - Virtual Try-On System</p>
                    </div>
                </body>
                </html>
            ",
            TextBody = $@"
                Password Reset Request
                
                We received a request to reset your password for your VTOS account.
                
                Click this link to reset your password: {resetLink}
                
                This link will expire in 1 hour.
                
                If you didn't request a password reset, please ignore this email.
                
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

    public async Task SendChangePasswordOTPEmailAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "VTOS - Password Change Verification Code";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>Password Change Request</h2>
                        <p>You have requested to change your password for your VTOS account.</p>
                        <p>Your verification code is:</p>
                        <div style='background-color: #f4f4f4; padding: 15px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 5px; margin: 20px 0;'>
                            {otpCode}
                        </div>
                        <p>This code will expire in <strong>10 minutes</strong>.</p>
                        <p style='color: #e74c3c;'>If you didn't request this change, please secure your account immediately.</p>
                        <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                        <p style='color: #888; font-size: 12px;'>VTOS - Virtual Try-On System</p>
                    </div>
                </body>
                </html>
            ",
            TextBody = $@"
                Password Change Request
                
                You have requested to change your password for your VTOS account.
                
                Your verification code is: {otpCode}
                
                This code will expire in 10 minutes.
                
                If you didn't request this change, please secure your account immediately.
                
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

    public async Task SendAccountCredentialsEmailAsync(string toEmail, string tempPassword, string roleName, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "VTOS - Your Account Has Been Created";

        var roleDisplay = roleName == "School" ? "Trường học" : "Nhà cung cấp";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #6938ef;'>Chào mừng đến VTOS!</h2>
                        <p>Tài khoản <strong>{roleDisplay}</strong> của bạn đã được tạo thành công.</p>
                        <div style='background-color: #f4f4f4; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                            <p style='margin: 5px 0;'><strong>Email đăng nhập:</strong> {toEmail}</p>
                            <p style='margin: 5px 0;'><strong>Mật khẩu tạm thời:</strong></p>
                            <div style='background-color: #fff; padding: 10px; text-align: center; font-size: 20px; font-weight: bold; letter-spacing: 2px; border: 1px solid #ddd; border-radius: 4px;'>
                                {tempPassword}
                            </div>
                        </div>
                        <p style='color: #e74c3c;'><strong>⚠️ Quan trọng:</strong> Vui lòng đổi mật khẩu ngay sau khi đăng nhập lần đầu.</p>
                        <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                        <p style='color: #888; font-size: 12px;'>VTOS - Virtual Try-On System</p>
                    </div>
                </body>
                </html>
            ",
            TextBody = $@"
                Chào mừng đến VTOS!
                
                Tài khoản {roleDisplay} của bạn đã được tạo thành công.
                
                Email đăng nhập: {toEmail}
                Mật khẩu tạm thời: {tempPassword}
                
                Vui lòng đổi mật khẩu ngay sau khi đăng nhập lần đầu.
                
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

    // ── Phase 02: Email Notification Templates ──

    public async Task SendOrderConfirmationEmailAsync(
        string toEmail, string parentName, string orderCode,
        decimal totalAmount, string campaignName,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"VTOS - Đặt hàng thành công #{orderCode}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <html><body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #10B981;'>✅ Đặt hàng thành công!</h2>
                    <p>Xin chào <strong>{parentName}</strong>,</p>
                    <p>Đơn hàng của bạn đã được thanh toán thành công.</p>
                    <div style='background-color: #f4f4f4; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 5px 0;'><strong>Mã đơn hàng:</strong> {orderCode}</p>
                        <p style='margin: 5px 0;'><strong>Chiến dịch:</strong> {campaignName}</p>
                        <p style='margin: 5px 0;'><strong>Tổng tiền:</strong> {totalAmount:N0}đ</p>
                    </div>
                    <p>Bạn có thể theo dõi trạng thái đơn hàng trong phần <strong>Lịch sử đặt hàng</strong> trên VTOS.</p>
                    <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                    <p style='color: #888; font-size: 12px;'>VTOS - Virtual Try-On System</p>
                </div>
                </body></html>",
            TextBody = $"Đặt hàng thành công! Mã: {orderCode}, Tổng: {totalAmount:N0}đ, Chiến dịch: {campaignName}"
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort,
            _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
        await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    public async Task SendPaymentDeadlineReminderAsync(
        string toEmail, string parentName, string orderCode,
        decimal amount, DateTime deadline,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"VTOS - ⏰ Nhắc nhở thanh toán đơn #{orderCode}";

        var deadlineStr = deadline.ToLocalTime().ToString("HH:mm dd/MM/yyyy");
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <html><body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #F59E0B;'>⏰ Nhắc nhở thanh toán</h2>
                    <p>Xin chào <strong>{parentName}</strong>,</p>
                    <p>Đơn hàng <strong>#{orderCode}</strong> của bạn sắp hết hạn thanh toán.</p>
                    <div style='background-color: #FFFBEB; padding: 15px; border-radius: 8px; margin: 20px 0; border: 1px solid #FCD34D;'>
                        <p style='margin: 5px 0;'><strong>Số tiền:</strong> {amount:N0}đ</p>
                        <p style='margin: 5px 0; color: #DC2626;'><strong>Hạn thanh toán:</strong> {deadlineStr}</p>
                    </div>
                    <p>Vui lòng hoàn tất thanh toán trước thời hạn để tránh đơn hàng bị hủy.</p>
                    <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                    <p style='color: #888; font-size: 12px;'>VTOS - Virtual Try-On System</p>
                </div>
                </body></html>",
            TextBody = $"Nhắc nhở: Đơn #{orderCode} ({amount:N0}đ) sắp hết hạn thanh toán lúc {deadlineStr}."
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort,
            _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
        await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    public async Task SendCampaignDeadlineReminderAsync(
        string toEmail, string parentName,
        string campaignName, string schoolName, DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"VTOS - 🔔 Chiến dịch \"{campaignName}\" sắp kết thúc";

        var endDateStr = endDate.ToLocalTime().ToString("HH:mm dd/MM/yyyy");
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <html><body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #6938EF;'>🔔 Chiến dịch sắp kết thúc!</h2>
                    <p>Xin chào <strong>{parentName}</strong>,</p>
                    <p>Chiến dịch đặt đồng phục từ trường <strong>{schoolName}</strong> sắp hết hạn.</p>
                    <div style='background-color: #F0EAFF; padding: 15px; border-radius: 8px; margin: 20px 0; border: 1px solid #C4B5FD;'>
                        <p style='margin: 5px 0;'><strong>Chiến dịch:</strong> {campaignName}</p>
                        <p style='margin: 5px 0;'><strong>Trường:</strong> {schoolName}</p>
                        <p style='margin: 5px 0; color: #DC2626;'><strong>Hết hạn:</strong> {endDateStr}</p>
                    </div>
                    <p>Hãy đặt hàng ngay trước khi chiến dịch kết thúc!</p>
                    <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                    <p style='color: #888; font-size: 12px;'>VTOS - Virtual Try-On System</p>
                </div>
                </body></html>",
            TextBody = $"Chiến dịch \"{campaignName}\" tại {schoolName} sắp hết hạn lúc {endDateStr}. Đặt hàng ngay!"
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort,
            _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
        await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    public async Task SendContractReplyNotificationAsync(
        string toEmail, string recipientName,
        string contractName, string action, string respondentName,
        CancellationToken cancellationToken = default)
    {
        var isApproved = action.Equals("Approved", StringComparison.OrdinalIgnoreCase);
        var actionVi = isApproved ? "đã được chấp thuận" : "đã bị từ chối";
        var color = isApproved ? "#10B981" : "#EF4444";
        var emoji = isApproved ? "✅" : "❌";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"VTOS - {emoji} Hợp đồng \"{contractName}\" {actionVi}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <html><body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: {color};'>{emoji} Hợp đồng {actionVi}</h2>
                    <p>Xin chào <strong>{recipientName}</strong>,</p>
                    <p>Hợp đồng <strong>{contractName}</strong> {actionVi} bởi <strong>{respondentName}</strong>.</p>
                    <div style='background-color: #f4f4f4; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 5px 0;'><strong>Hợp đồng:</strong> {contractName}</p>
                        <p style='margin: 5px 0;'><strong>Hành động:</strong> {actionVi}</p>
                        <p style='margin: 5px 0;'><strong>Bởi:</strong> {respondentName}</p>
                    </div>
                    <p>Đăng nhập VTOS để xem chi tiết.</p>
                    <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                    <p style='color: #888; font-size: 12px;'>VTOS - Virtual Try-On System</p>
                </div>
                </body></html>",
            TextBody = $"Hợp đồng \"{contractName}\" {actionVi} bởi {respondentName}."
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort,
            _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
        await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}

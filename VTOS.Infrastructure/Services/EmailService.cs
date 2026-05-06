using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net;
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

        if (roleName == "HomeroomTeacher")
        {
            roleDisplay = "Nhà cung cấp";
        }

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

    public async Task SendDirectOrderReceiptConfirmationEmailAsync(
        string toEmail,
        string parentName,
        string orderCode,
        DateTime orderDate,
        string providerName,
        decimal totalAmount,
        string? shippingCompany,
        string? trackingCode,
        string confirmUrl,
        IReadOnlyList<DirectOrderDeliveryEmailItem> items,
        CancellationToken cancellationToken = default)
    {
        var safeParentName = WebUtility.HtmlEncode(parentName);
        var safeOrderCode = WebUtility.HtmlEncode(orderCode);
        var safeProviderName = WebUtility.HtmlEncode(providerName);
        var safeShippingCompany = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(shippingCompany) ? "Chưa cập nhật" : shippingCompany);
        var safeTrackingCode = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(trackingCode) ? "Chưa cập nhật" : trackingCode);
        var safeConfirmUrl = WebUtility.HtmlEncode(confirmUrl);
        var orderDateText = orderDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

        var itemRows = string.Join("", items.Select((item, index) =>
        {
            var itemName = WebUtility.HtmlEncode(item.OutfitName);
            var itemSize = WebUtility.HtmlEncode(item.Size);
            var image = string.IsNullOrWhiteSpace(item.ImageUrl)
                ? ""
                : $@"<img src='{WebUtility.HtmlEncode(item.ImageUrl)}' alt='{itemName}' style='width: 96px; height: 96px; object-fit: cover; border-radius: 8px; border: 1px solid #333;' />";

            return $@"
                <tr>
                    <td style='padding: 18px 0; vertical-align: top; width: 110px;'>{image}</td>
                    <td style='padding: 18px 0; color: #f3f4f6; vertical-align: top;'>
                        <p style='margin: 0 0 8px; font-size: 15px; font-weight: 700;'>{index + 1}. {itemName}</p>
                        <p style='margin: 4px 0; color: #d1d5db;'>Kích cỡ: <strong>{itemSize}</strong></p>
                        <p style='margin: 4px 0; color: #d1d5db;'>Số lượng: <strong>{item.Quantity}</strong></p>
                        <p style='margin: 4px 0; color: #d1d5db;'>Giá: <strong>{item.UnitPrice:N0}đ</strong></p>
                    </td>
                </tr>";
        }));

        if (string.IsNullOrWhiteSpace(itemRows))
        {
            itemRows = "<tr><td style='padding: 18px 0; color: #d1d5db;'>Không có thông tin sản phẩm.</td></tr>";
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"VTOS - Xác nhận đã nhận đơn hàng #{orderCode}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <html>
                <body style='margin: 0; background: #111827; font-family: Arial, sans-serif; color: #f9fafb;'>
                    <div style='max-width: 680px; margin: 0 auto; padding: 28px 22px;'>
                        <div style='text-align: center; margin-bottom: 26px;'>
                            <div style='display: inline-block; color: #7c3aed; font-size: 30px; font-weight: 800;'>VTOS</div>
                        </div>

                        <p style='font-size: 17px; line-height: 1.6;'>Xin chào <strong>{safeParentName}</strong>,</p>
                        <p style='font-size: 17px; line-height: 1.6;'>
                            Đơn hàng <a href='{safeConfirmUrl}' style='color: #a78bfa; font-weight: 700;'>#{safeOrderCode}</a>
                            của bạn đang ở trạng thái giao hàng và chờ bạn xác nhận đã nhận.
                        </p>
                        <p style='font-size: 16px; line-height: 1.7; color: #e5e7eb;'>
                            Khi đã nhận được hàng, vui lòng đăng nhập VTOS để xác nhận trong vòng 7 ngày. Sau khi bạn xác nhận,
                            hệ thống sẽ ghi nhận đơn hàng đã hoàn tất và bắt đầu thời gian xử lý thanh toán cho nhà cung cấp
                            <a href='{safeConfirmUrl}' style='color: #a78bfa; font-weight: 700;'>{safeProviderName}</a>.
                            Nếu bạn không xác nhận trong thời gian này, VTOS sẽ tự động xác nhận đơn hàng theo chính sách hệ thống.
                        </p>

                        <div style='text-align: center; margin: 24px 0 30px;'>
                            <a href='{safeConfirmUrl}' style='display: inline-block; background: #7c3aed; color: #fff; padding: 13px 34px; text-decoration: none; border-radius: 6px; font-weight: 700;'>
                                Đã nhận hàng
                            </a>
                        </div>

                        <hr style='border: none; border-top: 1px solid #374151; margin: 28px 0;' />
                        <h3 style='font-size: 16px; letter-spacing: .04em; color: #f3f4f6;'>THÔNG TIN ĐƠN HÀNG - DÀNH CHO PHỤ HUYNH</h3>
                        <table style='width: 100%; margin: 14px 0 22px; color: #e5e7eb; border-collapse: collapse;'>
                            <tr><td style='padding: 4px 0; color: #9ca3af;'>Mã đơn hàng:</td><td style='padding: 4px 0;'><a href='{safeConfirmUrl}' style='color: #a78bfa;'>#{safeOrderCode}</a></td></tr>
                            <tr><td style='padding: 4px 0; color: #9ca3af;'>Ngày đặt hàng:</td><td style='padding: 4px 0;'>{orderDateText}</td></tr>
                            <tr><td style='padding: 4px 0; color: #9ca3af;'>Nhà cung cấp:</td><td style='padding: 4px 0;'>{safeProviderName}</td></tr>
                            <tr><td style='padding: 4px 0; color: #9ca3af;'>Đơn vị vận chuyển:</td><td style='padding: 4px 0;'>{safeShippingCompany}</td></tr>
                            <tr><td style='padding: 4px 0; color: #9ca3af;'>Mã vận đơn:</td><td style='padding: 4px 0;'>{safeTrackingCode}</td></tr>
                        </table>

                        <table style='width: 100%; border-collapse: collapse; border-top: 1px solid #374151; border-bottom: 1px solid #374151;'>
                            {itemRows}
                        </table>

                        <table style='width: 100%; margin: 22px 0; color: #f3f4f6; border-collapse: collapse;'>
                            <tr><td style='padding: 4px 0; color: #9ca3af;'>Tổng thanh toán:</td><td style='padding: 4px 0; text-align: right; font-weight: 800;'>{totalAmount:N0}đ</td></tr>
                        </table>

                        <hr style='border: none; border-top: 1px solid #374151; margin: 28px 0;' />
                        <h3 style='font-size: 16px; color: #f3f4f6;'>BƯỚC TIẾP THEO</h3>
                        <p style='font-size: 16px; line-height: 1.7; color: #e5e7eb;'>
                            Nếu sản phẩm có vấn đề, vui lòng liên hệ VTOS hoặc nhà cung cấp trước khi xác nhận đã nhận hàng.
                            Sau khi bạn nhấn <strong>Đã nhận hàng</strong>, đơn hàng sẽ chuyển sang trạng thái hoàn tất và bạn có thể đánh giá nhà cung cấp.
                        </p>
                        <p style='font-size: 15px; line-height: 1.6; color: #d1d5db;'>Cảm ơn bạn đã sử dụng VTOS.</p>
                    </div>
                </body>
                </html>",
            TextBody = $@"
Xin chào {parentName},

Đơn hàng #{orderCode} của bạn đang ở trạng thái giao hàng và chờ bạn xác nhận đã nhận.
Khi đã nhận được hàng, vui lòng xác nhận trong vòng 7 ngày tại: {confirmUrl}

Mã đơn hàng: #{orderCode}
Ngày đặt hàng: {orderDateText}
Nhà cung cấp: {providerName}
Đơn vị vận chuyển: {(string.IsNullOrWhiteSpace(shippingCompany) ? "Chưa cập nhật" : shippingCompany)}
Mã vận đơn: {(string.IsNullOrWhiteSpace(trackingCode) ? "Chưa cập nhật" : trackingCode)}
Tổng thanh toán: {totalAmount:N0}đ

Nếu sản phẩm có vấn đề, vui lòng liên hệ VTOS hoặc nhà cung cấp trước khi xác nhận đã nhận hàng.

VTOS"
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

    // ── Contract Signing OTP ──

    public async Task SendContractSignOTPAsync(
        string toEmail, string toName,
        string otpCode, string contractName, string contractNumber,
        int expiresInMinutes,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"VTOS - ✍️ Mã xác thực ký hợp đồng {contractNumber}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <html><body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #6938EF;'>✍️ Xác thực ký hợp đồng điện tử</h2>
                    <p>Xin chào <strong>{toName}</strong>,</p>
                    <p>Bạn đã yêu cầu ký hợp đồng điện tử sau:</p>
                    <div style='background-color: #F0EAFF; padding: 15px; border-radius: 8px; margin: 20px 0; border: 1px solid #C4B5FD;'>
                        <p style='margin: 5px 0;'><strong>Hợp đồng:</strong> {contractName}</p>
                        <p style='margin: 5px 0;'><strong>Số HĐ:</strong> {contractNumber}</p>
                    </div>
                    <p>Mã xác thực (OTP) của bạn là:</p>
                    <div style='background-color: #1A1A2E; color: #fff; padding: 20px; text-align: center; font-size: 36px; font-weight: bold; letter-spacing: 10px; margin: 20px 0; border-radius: 8px;'>
                        {otpCode}
                    </div>
                    <p>Mã có hiệu lực trong <strong>{expiresInMinutes} phút</strong>.</p>
                    <p style='color: #e74c3c;'>⚠️ Không chia sẻ mã này với bất kỳ ai. Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email này.</p>
                    <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                    <p style='color: #888; font-size: 12px;'>VTOS - Virtual Try-On System</p>
                </div>
                </body></html>",
            TextBody = $"Mã OTP ký hợp đồng {contractNumber}: {otpCode} (hiệu lực {expiresInMinutes} phút). Không chia sẻ mã này."
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort,
            _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
        await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    // ── Phase 03: Admin Notification Digest ──

    public async Task SendAdminDigestEmailAsync(
        string toEmail, string adminName,
        IReadOnlyList<(string Title, string Message, DateTime CreatedAt)> notifications,
        CancellationToken cancellationToken = default)
    {
        if (notifications.Count == 0) return;

        var notificationRows = string.Join("", notifications.Select(n =>
            $@"<tr>
                <td style='padding: 12px; border-bottom: 1px solid #eee;'>
                    <strong style='color: #1A1A2E;'>{n.Title}</strong><br/>
                    <span style='color: #666; font-size: 13px;'>{n.Message}</span><br/>
                    <span style='color: #999; font-size: 11px;'>{n.CreatedAt.ToLocalTime():HH:mm dd/MM/yyyy}</span>
                </td>
            </tr>"));

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"VTOS - 🔔 Bạn có {notifications.Count} thông báo chưa đọc";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <html><body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #6938EF;'>🔔 Thông báo từ VTOS</h2>
                    <p>Xin chào <strong>{adminName}</strong>,</p>
                    <p>Bạn có <strong>{notifications.Count} thông báo</strong> chưa xử lý:</p>
                    <table style='width: 100%; border-collapse: collapse; margin: 20px 0; border: 1px solid #ddd; border-radius: 8px;'>
                        {notificationRows}
                    </table>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='https://vtos.homes/admin/dashboard' style='background-color: #6938EF; color: white; padding: 14px 28px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Đăng nhập xử lý</a>
                    </div>
                    <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                    <p style='color: #888; font-size: 12px;'>VTOS - Virtual Try-On System</p>
                </div>
                </body></html>",
            TextBody = $"Bạn có {notifications.Count} thông báo chưa đọc trên VTOS. Đăng nhập để xử lý."
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort,
            _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
        await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    // ── Teacher Reminder ──

    public async Task SendTeacherReminderEmailAsync(
        string toEmail, string parentName,
        string teacherName, string className,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"VTOS - 📣 Nhắc nhở hoàn tất đơn đồng phục lớp {className}";

        var noteSection = string.IsNullOrWhiteSpace(note)
            ? ""
            : $@"<div style='background-color: #FFF8E1; padding: 12px 15px; border-radius: 8px; margin: 16px 0; border-left: 4px solid #FFA726;'>
                    <p style='margin: 0; font-size: 13px; color: #E65100; font-weight: bold;'>📝 Ghi chú từ giáo viên:</p>
                    <p style='margin: 6px 0 0; color: #333;'>{WebUtility.HtmlEncode(note)}</p>
                </div>";

        var noteText = string.IsNullOrWhiteSpace(note) ? "" : $"\nGhi chú: {note}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <html><body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #6938EF;'>📣 Nhắc nhở từ giáo viên chủ nhiệm</h2>
                    <p>Xin chào <strong>{WebUtility.HtmlEncode(parentName)}</strong>,</p>
                    <p>Giáo viên <strong>{WebUtility.HtmlEncode(teacherName)}</strong> nhắc phụ huynh hoàn tất đơn đồng phục cho lớp <strong>{WebUtility.HtmlEncode(className)}</strong>.</p>
                    <div style='background-color: #F0EAFF; padding: 15px; border-radius: 8px; margin: 20px 0; border: 1px solid #C4B5FD;'>
                        <p style='margin: 5px 0;'><strong>Giáo viên:</strong> {WebUtility.HtmlEncode(teacherName)}</p>
                        <p style='margin: 5px 0;'><strong>Lớp:</strong> {WebUtility.HtmlEncode(className)}</p>
                    </div>
                    {noteSection}
                    <p>Vui lòng đăng nhập VTOS để hoàn tất đặt đồng phục cho con em.</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='https://vtos.homes/my-orders' style='background-color: #6938EF; color: white; padding: 14px 28px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Đặt đồng phục ngay</a>
                    </div>
                    <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                    <p style='color: #888; font-size: 12px;'>VTOS - Virtual Try-On System</p>
                </div>
                </body></html>",
            TextBody = $"Giáo viên {teacherName} nhắc phụ huynh hoàn tất đơn đồng phục cho lớp {className}.{noteText}\n\nĐăng nhập VTOS để đặt hàng: https://vtos.homes/my-orders"
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort,
            _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
        await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    // ── Chat Digest ──

    public async Task SendChatDigestEmailAsync(
        string toEmail, string recipientName,
        string channelLabel, string channelType,
        IReadOnlyList<(string SenderName, string Content, DateTime SentAt)> messages,
        CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0) return;

        var channelTypeVi = channelType switch
        {
            "SupportTicket" => "Ticket hỗ trợ",
            "Contract" => "Hợp đồng",
            "ClassGroup" => "Nhóm lớp",
            _ => channelType
        };

        var messageRows = string.Join("", messages.Select(m =>
            $@"<tr>
                <td style='padding: 10px 12px; border-bottom: 1px solid #eee; vertical-align: top; width: 120px;'>
                    <strong style='color: #1A1A2E;'>{WebUtility.HtmlEncode(m.SenderName)}</strong><br/>
                    <span style='color: #999; font-size: 11px;'>{m.SentAt.ToLocalTime():HH:mm dd/MM}</span>
                </td>
                <td style='padding: 10px 12px; border-bottom: 1px solid #eee; color: #333;'>
                    {WebUtility.HtmlEncode(m.Content.Length > 200 ? m.Content[..200] + "…" : m.Content)}
                </td>
            </tr>"));

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"VTOS - 💬 {messages.Count} tin nhắn mới trong {channelTypeVi}: {channelLabel}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <html><body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #6938EF;'>💬 Tin nhắn mới trong cuộc trò chuyện</h2>
                    <p>Xin chào <strong>{WebUtility.HtmlEncode(recipientName)}</strong>,</p>
                    <p>Bạn có <strong>{messages.Count} tin nhắn mới</strong> trong <strong>{channelTypeVi}</strong>: <strong>{WebUtility.HtmlEncode(channelLabel)}</strong>.</p>
                    <table style='width: 100%; border-collapse: collapse; margin: 20px 0; border: 1px solid #ddd; border-radius: 8px;'>
                        {messageRows}
                    </table>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='https://vtos.homes' style='background-color: #6938EF; color: white; padding: 14px 28px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Xem cuộc trò chuyện</a>
                    </div>
                    <hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
                    <p style='color: #888; font-size: 12px;'>VTOS - Virtual Try-On System</p>
                </div>
                </body></html>",
            TextBody = $"Bạn có {messages.Count} tin nhắn mới trong {channelTypeVi}: {channelLabel}. Đăng nhập VTOS để xem."
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

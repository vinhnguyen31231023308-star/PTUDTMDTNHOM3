using System.Net;
using System.Net.Mail;
using HairNovaShop.Models;
using Microsoft.Extensions.Options;

namespace HairNovaShop.Services;

public interface IEmailService
{
    Task SendOTPEmailAsync(string email, string otpCode, OTPType type);
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task SendOTPEmailAsync(string email, string otpCode, OTPType type)
    {
        try
        {
            var smtpClient = new SmtpClient(_emailSettings.SmtpHost)
            {
                Port = int.Parse(_emailSettings.SmtpPort),
                Credentials = new NetworkCredential(_emailSettings.SmtpUser, _emailSettings.SmtpPassword),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = type == OTPType.Registration 
                    ? "Mã OTP đăng ký tài khoản HairNovaShop" 
                    : "Mã OTP đặt lại mật khẩu HairNovaShop",
                Body = GenerateOTPEmailBody(otpCode, type),
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            // Log error
            throw new Exception($"Lỗi gửi email: {ex.Message}", ex);
        }
    }

    private string GenerateOTPEmailBody(string otpCode, OTPType type)
    {
        var action = type == OTPType.Registration ? "đăng ký tài khoản" : "đặt lại mật khẩu";
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #0061E1; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .otp-box {{ background-color: #fff; border: 2px solid #0061E1; padding: 20px; text-align: center; margin: 20px 0; }}
        .otp-code {{ font-size: 32px; font-weight: bold; color: #0061E1; letter-spacing: 5px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>HairNovaShop</h1>
        </div>
        <div class='content'>
            <h2>Mã OTP {action}</h2>
            <p>Xin chào,</p>
            <p>Bạn đã yêu cầu {action}. Vui lòng sử dụng mã OTP sau đây:</p>
            <div class='otp-box'>
                <div class='otp-code'>{otpCode}</div>
            </div>
            <p>Mã OTP này có hiệu lực trong vòng 10 phút.</p>
            <p>Nếu bạn không yêu cầu {action}, vui lòng bỏ qua email này.</p>
        </div>
        <div class='footer'>
            <p>© 2025 HairNovaShop. Tất cả quyền được bảo lưu.</p>
        </div>
    </div>
</body>
</html>";
    }
}

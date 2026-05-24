using MailKit.Net.Smtp;        // ✅ Явно указываем MailKit
using MailKit.Security;
using MimeKit;

namespace EcoApp.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var host = _config["Smtp:Host"] ?? "smtp.mail.ru";
            var port = int.Parse(_config["Smtp:Port"] ?? "465");
            var username = _config["Smtp:Username"] ?? "ecoapp-belarus@mail.ru";
            var password = _config["Smtp:Password"] ?? "";
            var fromEmail = _config["Smtp:FromEmail"] ?? "ecoapp-belarus@mail.ru";
            var fromName = _config["Smtp:FromName"] ?? "EcoApp Belarus";

            // Если SMTP не настроен — логируем
            if (string.IsNullOrEmpty(password))
            {
                Console.WriteLine($"[EMAIL STUB] To: {toEmail}");
                Console.WriteLine($"[EMAIL STUB] Subject: {subject}");
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlContent
            };
            message.Body = bodyBuilder.ToMessageBody();

            // ✅ ИСПРАВЛЕНО: Явно указываем MailKit.Net.Smtp.SmtpClient
            using var client = new MailKit.Net.Smtp.SmtpClient();

            try
            {
                await client.ConnectAsync(host, port, SecureSocketOptions.SslOnConnect);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine($"[SMTP] Email sent to {toEmail}: {subject}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SMTP ERROR] {ex.Message}");
                throw;
            }
        }

        public async Task SendConfirmationEmailAsync(string toEmail, string token)
        {
            var baseUrl = _config["App:BaseUrl"] ?? "http://localhost:5287";
            var confirmationLink = $"{baseUrl}/api/auth/confirm-email?token={token}";

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background: #1a1a2e; color: #fff; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #2a2a3e; border-radius: 12px; padding: 30px; }}
        h1 {{ color: #4CAF50; }}
        .btn {{ display: inline-block; background: #6A0DAD; color: white; padding: 15px 30px; 
               text-decoration: none; border-radius: 8px; margin: 20px 0; }}
        .link {{ color: #888; word-break: break-all; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>🌿 EcoApp Belarus</h1>
        <h2>Подтверждение email</h2>
        <p>Привет! Вы зарегистрировались в EcoApp.</p>
        <p>Нажмите на кнопку ниже, чтобы подтвердить ваш email:</p>
        <a href='{confirmationLink}' class='btn'>Подтвердить email</a>
        <p>Или скопируйте ссылку:</p>
        <p class='link'>{confirmationLink}</p>
        <p style='color: #888; font-size: 12px;'>Ссылка действительна 24 часа.</p>
    </div>
</body>
</html>";

            await SendEmailAsync(toEmail, "Подтверждение регистрации EcoApp", html);
        }
    }
}
using MailKit.Net.Smtp;
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
            // ✅ Сначала пробуем переменные окружения Railway, потом appsettings
            var host = Environment.GetEnvironmentVariable("SMTP_HOST")
                ?? _config["Smtp:Host"]
                ?? "smtp.mail.ru";

            // ✅ Принудительно 465 + SSL для Mail.ru (Railway стабильнее работает)
            var portString = Environment.GetEnvironmentVariable("SMTP_PORT")
                ?? _config["Smtp:Port"]
                ?? "465";

            var port = int.Parse(portString);

            var username = Environment.GetEnvironmentVariable("SMTP_USERNAME")
                ?? _config["Smtp:Username"]
                ?? "ecoapp-belarus@mail.ru";

            var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD")
                ?? _config["Smtp:Password"]
                ?? "";

            var fromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL")
                ?? _config["Smtp:FromEmail"]
                ?? "ecoapp-belarus@mail.ru";

            var fromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME")
                ?? _config["Smtp:FromName"]
                ?? "EcoApp Belarus";

            // ✅ Детальное логирование
            Console.WriteLine($"[SMTP CONFIG] Host={host}, Port={port}, User={username}");
            Console.WriteLine($"[SMTP CONFIG] Password length={password.Length}, From={fromEmail}");

            if (string.IsNullOrEmpty(password))
            {
                Console.WriteLine($"[EMAIL STUB] Password is empty! To: {toEmail}");
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlContent };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new MailKit.Net.Smtp.SmtpClient();

            try
            {
                // ✅ Всегда SSL для Mail.ru (порт 465)
                var sslOptions = SecureSocketOptions.SslOnConnect;
                
                // ✅ Таймаут 15 секунд
                client.Timeout = 15000;

                Console.WriteLine($"[SMTP CONNECT] {host}:{port} with {sslOptions}, timeout={client.Timeout}ms");

                await client.ConnectAsync(host, port, sslOptions);

                Console.WriteLine($"[SMTP CONNECTED] {client.IsConnected}, {client.IsSecure}");

                await client.AuthenticateAsync(username, password);

                Console.WriteLine("[SMTP AUTHENTICATED] Sending email...");

                await client.SendAsync(message);

                Console.WriteLine($"[SMTP SUCCESS] Email sent to {toEmail}: {subject}");

                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SMTP ERROR] {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"[SMTP ERROR] Inner: {ex.InnerException?.Message}");
                throw;
            }
        }

        public async Task SendConfirmationEmailAsync(string toEmail, string token)
        {
            var baseUrl = Environment.GetEnvironmentVariable("RAILWAY_PUBLIC_DOMAIN")
                ?? _config["App:BaseUrl"]
                ?? "https://ecoapp-production-6393.up.railway.app";

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
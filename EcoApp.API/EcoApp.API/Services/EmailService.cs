using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EcoApp.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public EmailService(IConfiguration config)
        {
            _config = config;
            _httpClient = new HttpClient();
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            // ✅ Resend API Key
            var apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY")
                ?? _config["Resend:ApiKey"]
                ?? "";

            // ✅ Используем подтверждённый email как отправитель
            var fromEmail = "turkoludmila70@gmail.com";

            Console.WriteLine($"[EMAIL] Sending via Resend from {fromEmail} to {toEmail}");
            Console.WriteLine($"[EMAIL] API Key length: {apiKey.Length}");

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine($"[EMAIL STUB] API Key is empty! To: {toEmail}");
                return;
            }

            try
            {
                var url = "https://api.resend.com/emails";

                var json = JsonSerializer.Serialize(new
                {
                    from = fromEmail,
                    to = toEmail,
                    subject = subject,
                    html = htmlContent
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[EMAIL RESPONSE] Status: {response.StatusCode}");
                Console.WriteLine($"[EMAIL RESPONSE] Body: {responseBody}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Resend error: {responseBody}");
                }

                Console.WriteLine($"[EMAIL SUCCESS] Sent to {toEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL ERROR] {ex.GetType().Name}: {ex.Message}");
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
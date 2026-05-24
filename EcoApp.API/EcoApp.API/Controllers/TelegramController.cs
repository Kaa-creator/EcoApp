using EcoApp.API.Data;
using EcoApp.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcoApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TelegramController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TelegramService _telegramService;
        private readonly IConfiguration _config;

        public TelegramController(
            AppDbContext context,
            TelegramService telegramService,
            IConfiguration config)
        {
            _context = context;
            _telegramService = telegramService;
            _config = config;
        }

        // 🔔 Отправить уведомление админу вручную
        [HttpPost("notify-admin")]
        public async Task<IActionResult> NotifyAdmin([FromBody] NotifyRequest request)
        {
            await _telegramService.NotifyAdminAsync(request.Message);
            return Ok(new { message = "Уведомление отправлено" });
        }

        // 📢 Массовая рассылка о новом мероприятии
        [HttpPost("broadcast-event")]
        public async Task<IActionResult> BroadcastEvent([FromBody] BroadcastRequest request)
        {
            var subscribers = _context.TelegramSubscriptions
                .Where(t => t.NotifyEvents)
                .ToList();

            int sent = 0;
            foreach (var sub in subscribers)
            {
                await _telegramService.NotifyUserAsync(sub.UserId,
                    $"🎉 Новое мероприятие!\n\n" +
                    $"📌 {request.Title}\n" +
                    $"🏙️ {request.City}\n" +
                    $"📅 {request.Date:dd.MM.yyyy}\n\n" +
                    $"🔗 Откройте приложение для подробностей");

                sent++;
            }

            return Ok(new { message = $"Отправлено {sent} уведомлений" });
        }

        // 📰 Массовая рассылка о новой статье
        [HttpPost("broadcast-article")]
        public async Task<IActionResult> BroadcastArticle([FromBody] BroadcastRequest request)
        {
            var subscribers = _context.TelegramSubscriptions
                .Where(t => t.NotifyArticles)
                .ToList();

            int sent = 0;
            foreach (var sub in subscribers)
            {
                await _telegramService.NotifyUserAsync(sub.UserId,
                    $"📰 Новая статья!\n\n" +
                    $"📌 {request.Title}\n\n" +
                    $"🔗 Откройте приложение для прочтения");

                sent++;
            }

            return Ok(new { message = $"Отправлено {sent} уведомлений" });
        }

        // 📋 Массовая рассылка о новом задании
        [HttpPost("broadcast-task")]
        public async Task<IActionResult> BroadcastTask([FromBody] BroadcastRequest request)
        {
            var subscribers = _context.TelegramSubscriptions
                .Where(t => t.NotifyTasks)
                .ToList();

            int sent = 0;
            foreach (var sub in subscribers)
            {
                await _telegramService.NotifyUserAsync(sub.UserId,
                    $"📋 Новое задание!\n\n" +
                    $"📌 {request.Title}\n" +
                    $"🎁 {request.Points} баллов\n\n" +
                    $"🔗 Откройте приложение для выполнения");

                sent++;
            }

            return Ok(new { message = $"Отправлено {sent} уведомлений" });
        }
    }

    public class NotifyRequest
    {
        public string Message { get; set; } = "";
    }

    public class BroadcastRequest
    {
        public string Title { get; set; } = "";
        public string? City { get; set; }
        public DateTime? Date { get; set; }
        public int? Points { get; set; }
    }
}
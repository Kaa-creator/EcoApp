using EcoApp.API.Data;
using EcoApp.API.Models;
using EcoApp.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TelegramService _telegramService;

        public AdminController(AppDbContext context, TelegramService telegramService)
        {
            _context = context;
            _telegramService = telegramService;
        }

        // ============================================
        // 📊 СТАТИСТИКА
        // ============================================

        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            var stats = new
            {
                TotalUsers = _context.Users.Count(),
                TotalPoints = _context.Users.Sum(u => u.Points),
                TotalTasks = _context.EcoTasks.Count(),
                TotalReports = _context.TaskReports.Count(),
                PendingReports = _context.TaskReports.Count(r => r.Status == "Pending"),
                ApprovedReports = _context.TaskReports.Count(r => r.Status == "Approved"),
                TotalEvents = _context.EcoEvents.Count(e => e.IsActive),
                TotalArticles = _context.Articles.Count()
            };

            return Ok(stats);
        }

        // ============================================
        // 👤 ПОЛЬЗОВАТЕЛИ
        // ============================================

        [HttpGet("users")]
        public IActionResult GetUsers()
        {
            var users = _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Phone,
                    u.Role,
                    u.Points,
                    u.IsEmailConfirmed,
                    u.CreatedAt
                })
                .OrderByDescending(u => u.CreatedAt)
                .ToList();

            return Ok(users);
        }

        [HttpDelete("users/{id}")]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            if (user.Role == "Admin")
                return BadRequest("Нельзя удалить администратора");

            _context.Users.Remove(user);
            _context.SaveChanges();

            return Ok(new { message = "Пользователь удалён" });
        }

        // ============================================
        // 🗺️ ЭКО-ТОЧКИ (CRUD)
        // ============================================

        [HttpGet("ecopoints")]
        public IActionResult GetEcoPoints()
        {
            return Ok(_context.EcoPoints.ToList());
        }

        [HttpPost("ecopoints")]
        public IActionResult CreateEcoPoint(EcoPoint point)
        {
            _context.EcoPoints.Add(point);
            _context.SaveChanges();
            return Ok(point);
        }

        [HttpPut("ecopoints/{id}")]
        public IActionResult UpdateEcoPoint(int id, EcoPoint point)
        {
            var existing = _context.EcoPoints.Find(id);
            if (existing == null) return NotFound();

            existing.Name = point.Name;
            existing.Description = point.Description;
            existing.Category = point.Category;
            existing.Address = point.Address;
            existing.Latitude = point.Latitude;
            existing.Longitude = point.Longitude;
            existing.Phone = point.Phone;
            existing.Website = point.Website;

            _context.SaveChanges();
            return Ok(existing);
        }

        [HttpDelete("ecopoints/{id}")]
        public IActionResult DeleteEcoPoint(int id)
        {
            var point = _context.EcoPoints.Find(id);
            if (point == null) return NotFound();

            _context.EcoPoints.Remove(point);
            _context.SaveChanges();

            return Ok(new { message = "Точка удалена" });
        }

        // ============================================
        // 📋 ЗАДАНИЯ (CRUD) — С УВЕДОМЛЕНИЯМИ
        // ============================================

        [HttpGet("ecotasks")]
        public IActionResult GetEcoTasks()
        {
            return Ok(_context.EcoTasks.ToList());
        }

        [HttpPost("ecotasks")]
        public async Task<IActionResult> CreateEcoTask(EcoTask task)
        {
            _context.EcoTasks.Add(task);
            await _context.SaveChangesAsync();

            // ✅ РАССЫЛКА УВЕДОМЛЕНИЙ В TELEGRAM
            await _telegramService.BroadcastToSubscribersAsync("tasks",
                $"📋 Новое задание!\n\n" +
                $"📌 *{task.Title}*\n" +
                $"🎁 {task.Points} баллов\n" +
                $"🏷️ Категория: {task.Category}\n\n" +
                $"🔗 Откройте приложение для выполнения");

            return Ok(task);
        }

        [HttpPut("ecotasks/{id}")]
        public IActionResult UpdateEcoTask(int id, EcoTask task)
        {
            var existing = _context.EcoTasks.Find(id);
            if (existing == null) return NotFound();

            existing.Title = task.Title;
            existing.Description = task.Description;
            existing.Points = task.Points;
            existing.Category = task.Category;
            existing.RequiresPhoto = task.RequiresPhoto;

            _context.SaveChanges();
            return Ok(existing);
        }

        [HttpDelete("ecotasks/{id}")]
        public IActionResult DeleteEcoTask(int id)
        {
            var task = _context.EcoTasks.Find(id);
            if (task == null) return NotFound();

            _context.EcoTasks.Remove(task);
            _context.SaveChanges();

            return Ok(new { message = "Задание удалено" });
        }

        // ============================================
        // 📰 СТАТЬИ (CRUD) — С УВЕДОМЛЕНИЯМИ
        // ============================================

        [HttpGet("articles")]
        public IActionResult GetArticles()
        {
            return Ok(_context.Articles.ToList());
        }

        [HttpPost("articles")]
        public async Task<IActionResult> CreateArticle(Article article)
        {
            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            // ✅ РАССЫЛКА УВЕДОМЛЕНИЙ В TELEGRAM
            await _telegramService.BroadcastToSubscribersAsync("articles",
                $"📰 Новая статья!\n\n" +
                $"📌 *{article.Title}*\n" +
                $"🏷️ Категория: {article.Category}\n\n" +
                $"🔗 Откройте приложение для прочтения");

            return Ok(article);
        }

        [HttpPut("articles/{id}")]
        public IActionResult UpdateArticle(int id, Article article)
        {
            var existing = _context.Articles.Find(id);
            if (existing == null) return NotFound();

            existing.Title = article.Title;
            existing.Content = article.Content;
            existing.Category = article.Category;

            _context.SaveChanges();
            return Ok(existing);
        }

        [HttpDelete("articles/{id}")]
        public IActionResult DeleteArticle(int id)
        {
            var article = _context.Articles.Find(id);
            if (article == null) return NotFound();

            _context.Articles.Remove(article);
            _context.SaveChanges();

            return Ok(new { message = "Статья удалена" });
        }

        // ============================================
        // 🎉 ЭКО-МЕРОПРИЯТИЯ (CRUD) — С УВЕДОМЛЕНИЯМИ
        // ============================================

        [HttpGet("events")]
        public IActionResult GetEvents()
        {
            var events = _context.EcoEvents
                .OrderByDescending(e => e.EventDate)
                .ToList();

            return Ok(events);
        }

        [HttpPost("events")]
        public async Task<IActionResult> CreateEvent(EcoEvent ecoEvent)
        {
            ecoEvent.CreatedAt = DateTime.UtcNow;
            _context.EcoEvents.Add(ecoEvent);
            await _context.SaveChangesAsync();

            // ✅ РАССЫЛКА УВЕДОМЛЕНИЙ В TELEGRAM
            await _telegramService.BroadcastToSubscribersAsync("events",
                $"🎉 Новое мероприятие!\n\n" +
                $"📌 *{ecoEvent.Title}*\n" +
                $"🏙️ {ecoEvent.City ?? "Онлайн"}\n" +
                $"📅 {ecoEvent.EventDate:dd.MM.yyyy}\n\n" +
                $"🔗 Откройте приложение для подробностей");

            return Ok(ecoEvent);
        }

        [HttpPut("events/{id}")]
        public IActionResult UpdateEvent(int id, EcoEvent ecoEvent)
        {
            var existing = _context.EcoEvents.Find(id);
            if (existing == null) return NotFound();

            existing.Title = ecoEvent.Title;
            existing.Description = ecoEvent.Description;
            existing.City = ecoEvent.City;
            existing.Address = ecoEvent.Address;
            existing.EventDate = ecoEvent.EventDate;
            existing.EndDate = ecoEvent.EndDate;
            existing.Category = ecoEvent.Category;
            existing.ImageUrl = ecoEvent.ImageUrl;
            existing.Organizer = ecoEvent.Organizer;
            existing.ContactPhone = ecoEvent.ContactPhone;
            existing.Latitude = ecoEvent.Latitude;
            existing.Longitude = ecoEvent.Longitude;
            existing.IsActive = ecoEvent.IsActive;

            _context.SaveChanges();
            return Ok(existing);
        }

        [HttpDelete("events/{id}")]
        public IActionResult DeleteEvent(int id)
        {
            var ecoEvent = _context.EcoEvents.Find(id);
            if (ecoEvent == null) return NotFound();

            _context.EcoEvents.Remove(ecoEvent);
            _context.SaveChanges();

            return Ok(new { message = "Мероприятие удалено" });
        }
    }
}
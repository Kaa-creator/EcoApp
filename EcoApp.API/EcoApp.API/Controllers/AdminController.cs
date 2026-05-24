using EcoApp.API.Data;
using EcoApp.API.Models;
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

        public AdminController(AppDbContext context)
        {
            _context = context;
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

            // Нельзя удалить админа
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
        // 📋 ЗАДАНИЯ (CRUD)
        // ============================================

        [HttpGet("ecotasks")]
        public IActionResult GetEcoTasks()
        {
            return Ok(_context.EcoTasks.ToList());
        }

        [HttpPost("ecotasks")]
        public IActionResult CreateEcoTask(EcoTask task)
        {
            _context.EcoTasks.Add(task);
            _context.SaveChanges();
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
        // 📰 СТАТЬИ (CRUD)
        // ============================================

        [HttpGet("articles")]
        public IActionResult GetArticles()
        {
            return Ok(_context.Articles.ToList());
        }

        [HttpPost("articles")]
        public IActionResult CreateArticle(Article article)
        {
            _context.Articles.Add(article);
            _context.SaveChanges();
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
        // 🎉 ЭКО-МЕРОПРИЯТИЯ (CRUD)
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
        public IActionResult CreateEvent(EcoEvent ecoEvent)
        {
            ecoEvent.CreatedAt = DateTime.UtcNow;
            _context.EcoEvents.Add(ecoEvent);
            _context.SaveChanges();

            return Ok(ecoEvent);
        }

        [HttpPut("events/{id}")]
        public IActionResult UpdateEvent(int id, EcoEvent ecoEvent)
        {
            var existing = _context.EcoEvents.Find(id);
            if (existing == null) return NotFound();

            existing.Title = ecoEvent.Title;
            existing.Description = ecoEvent.Description;
            existing.City = ecoEvent.City;           // ✅ Может быть null
            existing.Address = ecoEvent.Address;     // ✅ Может быть null
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
        // ============================================
        // 🎉 ЭКО-МЕРОПРИЯТИЯ — УДАЛЕНИЕ (ДОБАВЛЕНО)
        // ============================================

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
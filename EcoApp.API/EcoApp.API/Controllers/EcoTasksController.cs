using Microsoft.AspNetCore.Mvc;
using EcoApp.API.Data;
using EcoApp.API.Models;
using EcoApp.API.Services;

namespace EcoApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EcoTasksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TelegramService _telegramService;  // ← ДОБАВИЛИ

        public EcoTasksController(AppDbContext context, TelegramService telegramService)
        {
            _context = context;
            _telegramService = telegramService;
        }

        [HttpGet]
        public IActionResult GetTasks([FromQuery] string? category = null)
        {
            var query = _context.EcoTasks.AsQueryable();
            if (!string.IsNullOrEmpty(category))
                query = query.Where(t => t.Category.ToLower() == category.ToLower());
            return Ok(query.ToList());
        }

        [HttpGet("{id}")]
        public IActionResult GetTask(int id)
        {
            var task = _context.EcoTasks.Find(id);
            if (task == null) return NotFound();
            return Ok(task);
        }

        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            var categories = _context.EcoTasks
                .Select(t => t.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask(EcoTask task)  // ← async
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

        [HttpPut("{id}")]
        public IActionResult UpdateTask(int id, EcoTask task)
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

        [HttpDelete("{id}")]
        public IActionResult DeleteTask(int id)
        {
            var task = _context.EcoTasks.Find(id);
            if (task == null) return NotFound();

            _context.EcoTasks.Remove(task);
            _context.SaveChanges();
            return Ok(new { message = "Задание удалено" });
        }
    }
}
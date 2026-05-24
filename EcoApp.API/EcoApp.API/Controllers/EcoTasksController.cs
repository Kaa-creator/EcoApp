using Microsoft.AspNetCore.Mvc;
using EcoApp.API.Data;
using EcoApp.API.Models;

namespace EcoApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EcoTasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EcoTasksController(AppDbContext context)
        {
            _context = context;
        }

        // 📋 Все задания (с фильтром по категории)
        [HttpGet]
        public IActionResult GetTasks([FromQuery] string? category = null)
        {
            var query = _context.EcoTasks.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(t => t.Category.ToLower() == category.ToLower());

            return Ok(query.ToList());
        }

        // 🔍 Одно задание по ID
        [HttpGet("{id}")]
        public IActionResult GetTask(int id)
        {
            var task = _context.EcoTasks.Find(id);
            if (task == null) return NotFound();
            return Ok(task);
        }

        // 🏷️ Список категорий
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
        public IActionResult CreateTask(EcoTask task)
        {
            _context.EcoTasks.Add(task);
            _context.SaveChanges();

            return Ok(task);
        }

        // ✏️ Обновление задания
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

        // 🗑️ Удаление задания
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
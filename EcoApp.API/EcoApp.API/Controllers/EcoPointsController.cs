using Microsoft.AspNetCore.Mvc;
using EcoApp.API.Data;
using EcoApp.API.Models;

namespace EcoApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EcoPointsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EcoPointsController(AppDbContext context)
        {
            _context = context;
        }

        // 🗺️ Все точки (с фильтром по категории)
        [HttpGet]
        public IActionResult GetPoints([FromQuery] string? category = null)
        {
            var query = _context.EcoPoints.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category.ToLower() == category.ToLower());

            return Ok(query.ToList());
        }

        // 🔍 Одна точка по ID
        [HttpGet("{id}")]
        public IActionResult GetPoint(int id)
        {
            var point = _context.EcoPoints.Find(id);
            if (point == null) return NotFound();
            return Ok(point);
        }

        // 🏷️ Список категорий
        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            var categories = _context.EcoPoints
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return Ok(categories);
        }

        [HttpPost]
        public IActionResult AddPoint(EcoPoint point)
        {
            _context.EcoPoints.Add(point);
            _context.SaveChanges();

            return Ok(point);
        }

        // ✏️ Обновление точки
        [HttpPut("{id}")]
        public IActionResult UpdatePoint(int id, EcoPoint point)
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

        // 🗑️ Удаление точки
        [HttpDelete("{id}")]
        public IActionResult DeletePoint(int id)
        {
            var point = _context.EcoPoints.Find(id);
            if (point == null) return NotFound();

            _context.EcoPoints.Remove(point);
            _context.SaveChanges();

            return Ok(new { message = "Точка удалена" });
        }
    }
}
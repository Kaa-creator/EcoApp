using EcoApp.API.Data;
using EcoApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EventsController(AppDbContext context)
        {
            _context = context;
        }

        // 📋 Все активные мероприятия
        [HttpGet]
        public IActionResult GetEvents([FromQuery] string? city = null, [FromQuery] string? category = null)
        {
            var query = _context.EcoEvents
                .Where(e => e.IsActive)
                .AsQueryable();

            // ✅ Фильтр по городу только если указан
            if (!string.IsNullOrEmpty(city))
                query = query.Where(e => e.City != null && e.City.ToLower() == city.ToLower());

            if (!string.IsNullOrEmpty(category))
                query = query.Where(e => e.Category.ToLower() == category.ToLower());

            var events = query
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    e.City,
                    e.Address,
                    e.EventDate,
                    e.EndDate,
                    e.Category,
                    e.ImageUrl,
                    e.Organizer,
                    e.ContactPhone,
                    e.Latitude,
                    e.Longitude
                })
                .ToList();

            return Ok(events);
        }

        // 🔍 Одно мероприятие по ID
        [HttpGet("{id}")]
        public IActionResult GetEvent(int id)
        {
            var ecoEvent = _context.EcoEvents.Find(id);
            if (ecoEvent == null || !ecoEvent.IsActive) return NotFound();

            return Ok(ecoEvent);
        }

        // 🏙️ Список городов (только непустые)
        [HttpGet("cities")]
        public IActionResult GetCities()
        {
            var cities = _context.EcoEvents
                .Where(e => e.IsActive && !string.IsNullOrEmpty(e.City))
                .Select(e => e.City)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return Ok(cities);
        }

        // 🏷️ Список категорий
        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            var categories = _context.EcoEvents
                .Where(e => e.IsActive)
                .Select(e => e.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return Ok(categories);
        }
    }
}
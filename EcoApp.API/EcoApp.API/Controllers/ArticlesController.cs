using Microsoft.AspNetCore.Mvc;
using EcoApp.API.Data;
using EcoApp.API.Models;
using EcoApp.API.Services;

namespace EcoApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticlesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TelegramService _telegramService;  // ← ДОБАВИЛИ

        public ArticlesController(AppDbContext context, TelegramService telegramService)
        {
            _context = context;
            _telegramService = telegramService;
        }

        [HttpGet]
        public IActionResult GetArticles([FromQuery] string? category = null)
        {
            var query = _context.Articles.AsQueryable();
            if (!string.IsNullOrEmpty(category))
                query = query.Where(a => a.Category.ToLower() == category.ToLower());
            return Ok(query.ToList());
        }

        [HttpGet("{id}")]
        public IActionResult GetArticle(int id)
        {
            var article = _context.Articles.Find(id);
            if (article == null) return NotFound();
            return Ok(article);
        }

        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            var categories = _context.Articles
                .Select(a => a.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> CreateArticle(Article article)  // ← async
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

        [HttpPut("{id}")]
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

        [HttpDelete("{id}")]
        public IActionResult DeleteArticle(int id)
        {
            var article = _context.Articles.Find(id);
            if (article == null) return NotFound();

            _context.Articles.Remove(article);
            _context.SaveChanges();
            return Ok(new { message = "Статья удалена" });
        }
    }
}
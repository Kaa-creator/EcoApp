using Microsoft.AspNetCore.Mvc;
using EcoApp.API.Data;
using EcoApp.API.Models;

namespace EcoApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticlesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ArticlesController(AppDbContext context)
        {
            _context = context;
        }

        // 📰 Все статьи (с фильтром по категории)
        [HttpGet]
        public IActionResult GetArticles([FromQuery] string? category = null)
        {
            var query = _context.Articles.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(a => a.Category.ToLower() == category.ToLower());

            return Ok(query.ToList());
        }

        // 🔍 Одна статья по ID
        [HttpGet("{id}")]
        public IActionResult GetArticle(int id)
        {
            var article = _context.Articles.Find(id);
            if (article == null) return NotFound();
            return Ok(article);
        }

        // 🏷️ Список категорий
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
        public IActionResult CreateArticle(Article article)
        {
            _context.Articles.Add(article);
            _context.SaveChanges();

            return Ok(article);
        }

        // ✏️ Обновление статьи
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

        // 🗑️ Удаление статьи
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
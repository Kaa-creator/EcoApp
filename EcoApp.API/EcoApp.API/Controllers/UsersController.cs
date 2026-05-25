using EcoApp.API.Data;
using EcoApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetUsers()
        {
            var users = _context.Users.ToList();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public IActionResult GetUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpGet("leaderboard")]
        public IActionResult GetLeaderboard()
        {
            var users = _context.Users
                .Where(u => u.Role != "Admin")
                .OrderByDescending(u => u.Points)
                .ToList();
            return Ok(users);
        }

        // ✅ ИЗМЕНЕНИЕ ПОЛЬЗОВАТЕЛЯ
        [HttpPut("{id}")]
        public IActionResult UpdateUser(int id, [FromBody] UpdateUserModel model)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            user.Name = model.Name;
            user.Email = model.Email;
            user.Phone = model.Phone;

            _context.SaveChanges();
            return Ok(user);
        }

        // ✅ УДАЛЕНИЕ ПОЛЬЗОВАТЕЛЯ
        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            // Нельзя удалить админа
            if (user.Role == "Admin")
                return BadRequest(new { message = "Нельзя удалить администратора" });

            _context.Users.Remove(user);
            _context.SaveChanges();

            return Ok(new { message = "Пользователь удалён" });
        }

        [HttpPut("{id}/password")]
        public IActionResult ChangePassword(int id, [FromBody] ChangePasswordModel model)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            if (!BCrypt.Net.BCrypt.Verify(model.OldPassword, user.PasswordHash))
                return BadRequest("Неверный текущий пароль");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            _context.SaveChanges();

            return Ok(new { message = "Пароль изменен" });
        }

        [HttpGet("{id}/stats")]
        public IActionResult GetUserStats(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            var completedTasks = _context.TaskReports
                .Count(r => r.UserId == id && r.Status == "Approved");

            var pendingTasks = _context.TaskReports
                .Count(r => r.UserId == id && r.Status == "Pending");

            var totalEarned = _context.TaskReports
                .Where(r => r.UserId == id && r.Status == "Approved")
                .Join(_context.EcoTasks,
                    r => r.TaskId,
                    t => t.Id,
                    (r, t) => t.Points)
                .Sum();

            return Ok(new
            {
                totalPoints = user.Points,
                completedTasks,
                pendingTasks,
                totalEarned
            });
        }

        // ============================================
        // TELEGRAM ПОДПИСКА
        // ============================================

        [HttpGet("{id}/telegram-status")]
        public IActionResult GetTelegramStatus(int id)
        {
            var subscription = _context.TelegramSubscriptions
                .FirstOrDefault(t => t.UserId == id);

            return Ok(new
            {
                isSubscribed = subscription != null
            });
        }

        [HttpDelete("{id}/telegram-unsubscribe")]
        public IActionResult UnsubscribeTelegram(int id)
        {
            var subscriptions = _context.TelegramSubscriptions
                .Where(t => t.UserId == id)
                .ToList();

            if (!subscriptions.Any())
                return NotFound(new { message = "Подписка не найдена" });

            _context.TelegramSubscriptions.RemoveRange(subscriptions);
            _context.SaveChanges();

            return Ok(new { message = "Вы отписались от уведомлений" });
        }
    }

    public class UpdateUserModel
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
    }

    public class ChangePasswordModel
    {
        public string OldPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}
using EcoApp.API.Data;
using EcoApp.API.Models;
using EcoApp.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly TelegramService _telegramService;

        public TaskReportsController(
            AppDbContext context,
            IWebHostEnvironment environment,
            TelegramService telegramService)
        {
            _context = context;
            _environment = environment;
            _telegramService = telegramService;
        }

        [HttpGet]
        public IActionResult GetReports()
        {
            var reports = _context.TaskReports.ToList();
            return Ok(reports);
        }

        // Получить отчеты на проверке (для админа)
        [HttpGet("pending")]
        public IActionResult GetPendingReports()
        {
            var reports = _context.TaskReports
                .Where(r => r.Status == "Pending")
                .Select(r => new {
                    r.Id,
                    r.UserId,
                    UserName = _context.Users.FirstOrDefault(u => u.Id == r.UserId)!.Name,
                    r.TaskId,
                    TaskTitle = _context.EcoTasks.FirstOrDefault(t => t.Id == r.TaskId)!.Title,
                    TaskPoints = _context.EcoTasks.FirstOrDefault(t => t.Id == r.TaskId)!.Points,
                    r.Comment,
                    r.PhotoUrl,
                    r.CreatedAt
                })
                .ToList();

            return Ok(reports);
        }

        // Получить историю проверенных
        [HttpGet("history")]
        public IActionResult GetHistory([FromQuery] string? status = null)
        {
            var query = _context.TaskReports
                .Where(r => r.Status == "Approved" || r.Status == "Rejected")
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            var reports = query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    r.Id,
                    r.UserId,
                    UserName = _context.Users.FirstOrDefault(u => u.Id == r.UserId)!.Name,
                    r.TaskId,
                    TaskTitle = _context.EcoTasks.FirstOrDefault(t => t.Id == r.TaskId)!.Title,
                    TaskPoints = _context.EcoTasks.FirstOrDefault(t => t.Id == r.TaskId)!.Points,
                    r.Status,
                    r.Comment,
                    r.CreatedAt,
                    ProcessedAt = r.CreatedAt
                })
                .ToList();

            return Ok(reports);
        }

        // Получить отчеты по конкретному пользователю
        [HttpGet("by-user/{userId}")]
        public IActionResult GetReportsByUser(int userId)
        {
            var reports = _context.TaskReports
                .Where(r => r.UserId == userId)
                .Select(r => new {
                    r.Id,
                    r.TaskId,
                    TaskTitle = _context.EcoTasks.FirstOrDefault(t => t.Id == r.TaskId)!.Title,
                    TaskPoints = _context.EcoTasks.FirstOrDefault(t => t.Id == r.TaskId)!.Points,
                    r.Status,
                    r.Comment,
                    PhotoUrl = r.PhotoUrl,
                    FullPhotoUrl = $"http://localhost:5287{r.PhotoUrl}",
                    r.CreatedAt
                })
                .ToList();

            return Ok(reports);
        }

        // Проверить, можно ли повторить отклоненное задание
        [HttpGet("can-retry/{reportId}")]
        public IActionResult CanRetry(int reportId)
        {
            var report = _context.TaskReports.Find(reportId);
            if (report == null) return NotFound();

            if (report.Status != "Rejected" || report.RejectedAt == null)
                return Ok(new { canRetry = false, secondsRemaining = 0 });

            var elapsed = DateTime.UtcNow - report.RejectedAt.Value;
            var cooldown = TimeSpan.FromMinutes(10);

            if (elapsed >= cooldown)
            {
                _context.TaskReports.Remove(report);
                _context.SaveChanges();

                return Ok(new { canRetry = true, secondsRemaining = 0 });
            }

            var remaining = cooldown - elapsed;
            return Ok(new
            {
                canRetry = false,
                secondsRemaining = (int)remaining.TotalSeconds
            });
        }

        // Обычная отправка без фото
        [HttpPost]
        public async Task<IActionResult> CreateReport(TaskReport report)
        {
            report.Status = "Pending";
            report.CreatedAt = DateTime.UtcNow;

            _context.TaskReports.Add(report);
            await _context.SaveChangesAsync();

            var task = await _context.EcoTasks.FindAsync(report.TaskId);
            var user = await _context.Users.FindAsync(report.UserId);
            await _telegramService.NotifyAdminAsync(
                $"Новый отчет!\n" +
                $"Пользователь: {user?.Name} (ID: {report.UserId})\n" +
                $"Задание: {task?.Title}\n" +
                $"Комментарий: {report.Comment}");

            return Ok(report);
        }

        // Отправка с фото
        [HttpPost("upload")]
        public async Task<IActionResult> CreateReportWithPhoto(
            [FromForm] int userId,
            [FromForm] int taskId,
            [FromForm] string comment,
            IFormFile? photo)
        {
            string? photoUrl = null;

            if (photo != null && photo.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{photo.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(fileStream);
                }

                photoUrl = $"/uploads/{uniqueFileName}";
            }

            var taskReport = new TaskReport
            {
                UserId = userId,
                TaskId = taskId,
                PhotoUrl = photoUrl ?? "",
                Comment = comment,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.TaskReports.Add(taskReport);
            await _context.SaveChangesAsync();

            var task = await _context.EcoTasks.FindAsync(taskId);
            var user = await _context.Users.FindAsync(userId);
            await _telegramService.NotifyAdminAsync(
                $"Новый отчет с фото!\n" +
                $"Пользователь: {user?.Name} (ID: {userId})\n" +
                $"Задание: {task?.Title}\n" +
                $"Комментарий: {comment}");

            return Ok(taskReport);
        }

        // ОДОБРИТЬ
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveReport(int id)
        {
            var report = _context.TaskReports.Find(id);
            if (report == null) return NotFound();

            report.Status = "Approved";

            var task = _context.EcoTasks.Find(report.TaskId);
            var user = _context.Users.Find(report.UserId);

            if (task != null && user != null)
            {
                user.Points += task.Points;
            }

            await _context.SaveChangesAsync();

            await _telegramService.NotifyUserAsync(
                report.UserId,
                $"Задание одобрено!\n" +
                $"Задание: {task?.Title}\n" +
                $"Начислено: {task?.Points} баллов\n" +
                $"Всего баллов: {user?.Points}");

            return Ok(report);
        }

        // ОТКЛОНИТЬ
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> RejectReport(int id)
        {
            var report = _context.TaskReports.Find(id);
            if (report == null) return NotFound();

            report.Status = "Rejected";
            report.RejectedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var task = await _context.EcoTasks.FindAsync(report.TaskId);
            await _telegramService.NotifyUserAsync(
                report.UserId,
                $"Задание отклонено\n" +
                $"Задание: {task?.Title}\n" +
                $"Можно повторить через 10 минут.");

            return Ok(report);
        }
    }
}
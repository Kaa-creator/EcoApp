using EcoApp.API.Data;
using EcoApp.API.Models;
using EcoApp.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EcoApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly EmailService _emailService;

        public AuthController(AppDbContext context, IConfiguration config, EmailService emailService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
        }

        // 📝 РЕГИСТРАЦИЯ — без подтверждения email
        [HttpPost("register")]
        public async Task<IActionResult> Register(User user)
        {
            // Проверяем, нет ли такого email
            if (_context.Users.Any(u => u.Email == user.Email))
                return BadRequest("Пользователь с таким email уже существует");

            // Хешируем пароль
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            user.Role = "User";
            user.Points = 0;
            user.IsEmailConfirmed = true; // ✅ Сразу подтверждаем email

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // ✅ Email подтверждение отключено — сразу возвращаем успех
            return Ok(new { message = "Регистрация успешна! Теперь можете войти." });
        }

        // ✅ ПОДТВЕРЖДЕНИЕ EMAIL — оставляем для обратной совместимости (не используется)
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string token)
        {
            return Ok(new { message = "Email подтверждение отключено. Войдите напрямую." });
        }

        // 🔐 ВХОД с JWT + роль
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            if (model == null)
                return BadRequest("Пустой запрос");

            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                return BadRequest("Email или пароль не пришли");

            var user = _context.Users
                .FirstOrDefault(u => u.Email == model.Email);

            if (user == null)
                return Unauthorized("Неверный логин или пароль");

            // Проверяем хеш пароля
            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                return Unauthorized("Неверный логин или пароль");

            // ✅ Проверка подтверждения email отключена

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]!);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new
            {
                token = tokenHandler.WriteToken(token),
                userId = user.Id,
                userName = user.Name,
                email = user.Email,
                role = user.Role
            });
        }
    }
}
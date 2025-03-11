using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LanguageLearningApp.Api.Application.Settings;
using LanguageLearningApp.Api.Domain.Entities;
using LanguageLearningApp.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LanguageLearningApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtSettings _jwtSettings;

        public AuthController(AppDbContext context, IOptions<JwtSettings> jwtSettings)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
        }

        // 1) REGISTER
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            // Kullanıcı adı veya email kontrolü
            var existingUser = await _context.Users
                .AnyAsync(u => u.Username == model.Username || u.Email == model.Email);

            if (existingUser)
            {
                return BadRequest("Username or Email is already in use.");
            }

            // Yeni user oluştur
            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                // Burada basit bir hash kullandık, daha güvenli hashing kütüphaneleri kullanılabilir
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully.");
        }

        // 2) LOGIN
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            // Kullanıcı var mı kontrolü
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            // Şifre doğrulama
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return Unauthorized("Invalid password.");
            }

            // JWT Token üretelim
            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        // Örnek korumalı bir endpoint
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            // 1) Kullanıcı gerçekten doğrulanmış mı?
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized("User not authenticated.");
            }

            // 2) NameIdentifier claim’i var mı?
            var nameIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(nameIdValue))
            {
                return Unauthorized("NameIdentifier claim is missing.");
            }

            // 3) Kullanıcı ID'si parse edilebiliyor mu?
            if (!int.TryParse(nameIdValue, out var userId))
            {
                return Unauthorized("Invalid user ID in token.");
            }

            // 4) Veritabanında kullanıcı var mı?
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.CreatedAt
            });
        }


        // Token üretme metodu
        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

            // Token'a koymak istediğimiz claim'ler
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                // Ek claim'ler eklenebilir
            };

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256
            );

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2), // Token geçerlilik süresi
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = creds
            };

            var tokenObj = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(tokenObj);
        }
    }

    // DTO Sınıfları
    public class RegisterDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public class LoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
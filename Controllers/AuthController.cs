using Microsoft.AspNetCore.Mvc;
using FoodiGOAPI.Services;
using FoodiGOAPI.Data;
using FoodiGOAPI.DTO;
using FoodiGOAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodiGOAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;   // ✅ added

        public AuthController(AppDbContext context, ITokenService tokenService, IEmailService emailService)
        {
            _context = context;
            _tokenService = tokenService;
            _emailService = emailService;               // ✅ injected
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password" });

            if (!user.IsActive)
                return Unauthorized(new { message = "Account disabled" });

            var token = _tokenService.CreateToken(user);
            return Ok(new LoginResponseDto
            {
                Token = token,
                Role = user.Role,
                FullName = user.FullName,
                UserId = user.Id
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
                return BadRequest(new { message = "Email already registered" });

            var role = registerDto.Role == "admin" ? "user" : registerDto.Role;

            var user = new User
            {
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                FullName = registerDto.FullName,
                Role = role,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = _tokenService.CreateToken(user);
            return Ok(new LoginResponseDto
            {
                Token = token,
                Role = user.Role,
                FullName = user.FullName,
                UserId = user.Id
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                return Ok(new { message = "If your email is registered, you will receive a reset link." });
            }

            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "-").Replace("/", "_").Replace("=", "");

            var expiry = DateTime.UtcNow.AddHours(1);

            var resetEntry = new PasswordReset
            {
                UserId = user.Id,
                Token = token,
                ExpiryTime = expiry,
                IsUsed = false
            };
            _context.PasswordResets.Add(resetEntry);
            await _context.SaveChangesAsync();

            var resetLink = $"https://yourapp.com/reset-password?email={Uri.EscapeDataString(user.Email)}&token={token}";

            await _emailService.SendResetEmailAsync(user.Email, resetLink);

            return Ok(new { message = "If your email is registered, you will receive a reset link." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return BadRequest(new { message = "Invalid request" });

            var resetEntry = await _context.PasswordResets
                .FirstOrDefaultAsync(r => r.UserId == user.Id && r.Token == dto.Token && !r.IsUsed);

            if (resetEntry == null || resetEntry.ExpiryTime < DateTime.UtcNow)
                return BadRequest(new { message = "Invalid or expired token" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            resetEntry.IsUsed = true;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password has been reset successfully." });
        }
    }
}
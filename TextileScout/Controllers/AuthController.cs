using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TextileScout.Web.Data;
using TextileScout.Web.DTOs;
using TextileScout.Web.Models;
using TextileScout.Web.Services;

namespace TextileScout.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;

        public AuthController(AppDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login() => View();

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);

            // BCRYPT ŞİFRE DOĞRULAMA
            if (user != null && BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Username, user.Role);
                var refreshToken = _tokenService.GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _context.SaveChangesAsync();

                SetTokenCookies(accessToken, refreshToken);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Kullanıcı adı veya şifre hatalı!";
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register() => View();

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
            {
                ViewBag.Error = "Şifreler eşleşmiyor!";
                return View();
            }

            bool exists = await _context.Users.AnyAsync(u => u.Username == dto.Username);
            if (exists)
            {
                ViewBag.Error = "Bu kullanıcı adı zaten alınmış!";
                return View();
            }

            // BCRYPT İLE ŞİFREYİ HASHLEME (Tuzlama/Salting otomatik yapılır)
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var newUser = new User
            {
                Username = dto.Username,
                PasswordHash = hashedPassword, // $2a$11$... şeklinde hash kaydolur
                Role = "User"
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Otomatik Giriş Yaptır
            var accessToken = _tokenService.GenerateAccessToken(newUser.Id, newUser.Username, newUser.Role);
            var refreshToken = _tokenService.GenerateRefreshToken();

            newUser.RefreshToken = refreshToken;
            newUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            SetTokenCookies(accessToken, refreshToken);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);

            ViewBag.SiteCount = await _context.TargetSites.CountAsync(s => s.UserId == userId);
            ViewBag.ProductCount = await _context.Products.CountAsync(p => p.UserId == userId);

            return View(user);
        }

        public async Task<IActionResult> Logout()
        {
            Response.Cookies.Delete("X-Access-Token");
            Response.Cookies.Delete("X-Refresh-Token");
            return RedirectToAction("Login");
        }

        private void SetTokenCookies(string accessToken, string refreshToken)
        {
            Response.Cookies.Append("X-Access-Token", accessToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTime.UtcNow.AddMinutes(15) });
            Response.Cookies.Append("X-Refresh-Token", refreshToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTime.UtcNow.AddDays(7) });
        }
    }
}
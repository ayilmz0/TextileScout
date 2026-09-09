using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TextileScout.Web.Data;
using TextileScout.Web.Models;

namespace TextileScout.Web.Controllers
{
    [Authorize]
    public class TargetSitesController : Controller
    {
        private readonly AppDbContext _context;

        public TargetSitesController(AppDbContext context)
        {
            _context = context;
        }

        // Giriş yapan kullanıcının ID'sini güvenli şekilde alan yardımcı mülk
        private int CurrentUserId
        {
            get
            {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier);
                return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
            }
        }

        public async Task<IActionResult> Index()
        {
            int userId = CurrentUserId;

            if (userId == 0)
            {
                // Kullanıcı kimliği doğrulanamadıysa Login'e yönlendir
                return RedirectToAction("Login", "Auth");
            }

            // Sadece oturum açmış kullanıcının sitelerini getir
            var sites = await _context.TargetSites
                .Where(s => s.UserId == userId)
                .ToListAsync();

            return View(sites);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string name, string url)
        {
            int userId = CurrentUserId;

            if (userId == 0)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(url))
            {
                _context.TargetSites.Add(new TargetSite
                {
                    Name = name,
                    Url = url,
                    UserId = userId
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            int userId = CurrentUserId;

            var site = await _context.TargetSites
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (site != null)
            {
                _context.TargetSites.Remove(site);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}
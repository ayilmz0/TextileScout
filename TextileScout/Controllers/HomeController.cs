using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TextileScout.Web.Data;
using TextileScout.Web.DTOs;

namespace TextileScout.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public HomeController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int CurrentUserId
        {
            get
            {
                var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
            }
        }

        public async Task<IActionResult> Index(string? site, string? timeRange, int page = 1)
        {
            int userId = CurrentUserId;

            // 1. Kullanıcıya ait ve henüz onaylanmamış ürünleri çekiyoruz
            var query = _context.Products.Where(p => p.UserId == userId && !p.IsApproved);

            // 2. Marka (Site) Filtresi
            if (!string.IsNullOrEmpty(site))
            {
                query = query.Where(p => p.SourceSite == site);
            }

            // 3. Zaman Filtresi
            if (timeRange == "24h")
                query = query.Where(p => p.DetectedAt >= DateTime.Now.AddDays(-1));
            else if (timeRange == "7d")
                query = query.Where(p => p.DetectedAt >= DateTime.Now.AddDays(-7));

            var products = await query.OrderByDescending(p => p.DetectedAt).ToListAsync();

            // 4. Kullanıcının takip ettiği marka listesi (Dropdown için)
            var availableSites = await _context.TargetSites
                                            .Where(s => s.UserId == userId)
                                            .Select(s => s.Name)
                                            .Distinct()
                                            .ToListAsync();

            // 5. View'ın beklediği modeli oluşturup gönderiyoruz
            var viewModel = new ProductListViewModel
            {
                Products = products,
                SelectedSite = site,
                TimeRange = timeRange,
                AvailableSites = availableSites
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Approved(int page = 1)
        {
            int pageSize = 12;
            var query = _context.Products.Where(p => p.IsApproved);

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var approvedProducts = await query
                .OrderByDescending(p => p.DetectedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new ProductListViewModel
            {
                Products = approvedProducts,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsApproved = true;
                product.Status = "Approved";
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id, string returnUrl)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var filePath = Path.Combine(_env.WebRootPath, product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TextileScout.Web.Data;
using TextileScout.Web.DTOs;

namespace TextileScout.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IDistributedCache _cache;

        public HomeController(AppDbContext context, IWebHostEnvironment env, IDistributedCache cache)
        {
            _context = context;
            _env = env;
            _cache = cache;
        }

        [AllowAnonymous]
        public IActionResult Landing()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index");
            }
            return View();
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

            // 1. Her kullanıcı, filtre ve sayfa kombinasyonuna özel benzersiz Cache Key
            string cacheKey = $"user_{userId}_index_site_{site ?? "all"}_time_{timeRange ?? "all"}_page_{page}";

            // 2. Önce Redis Cache kontrolü
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedModel = JsonSerializer.Deserialize<ProductListViewModel>(cachedData);
                if (cachedModel != null)
                {
                    return View(cachedModel);
                }
            }

            // 3. Cache yoksa SQL Server'dan sorgulama
            var query = _context.Products.Where(p => p.UserId == userId && !p.IsApproved);

            if (!string.IsNullOrEmpty(site))
            {
                query = query.Where(p => p.SourceSite == site);
            }

            if (timeRange == "24h")
                query = query.Where(p => p.DetectedAt >= DateTime.Now.AddDays(-1));
            else if (timeRange == "7d")
                query = query.Where(p => p.DetectedAt >= DateTime.Now.AddDays(-7));

            var products = await query.OrderByDescending(p => p.DetectedAt).ToListAsync();

            var availableSites = await _context.TargetSites
                                            .Where(s => s.UserId == userId)
                                            .Select(s => s.Name)
                                            .Distinct()
                                            .ToListAsync();

            var viewModel = new ProductListViewModel
            {
                Products = products,
                SelectedSite = site,
                TimeRange = timeRange,
                AvailableSites = availableSites
            };

            // 4. Sonucu 3 dakikalığına Redis'e yazma
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(viewModel), cacheOptions);

            return View(viewModel);
        }

        public async Task<IActionResult> Approved(int page = 1)
        {
            int userId = CurrentUserId;
            string cacheKey = $"user_{userId}_approved_page_{page}";

            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedModel = JsonSerializer.Deserialize<ProductListViewModel>(cachedData);
                if (cachedModel != null)
                {
                    return View(cachedModel);
                }
            }

            int pageSize = 12;
            var query = _context.Products.Where(p => p.UserId == userId && p.IsApproved);

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

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(viewModel), cacheOptions);

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

                await ClearUserProductsCache();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id, string? returnUrl)
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

                await ClearUserProductsCache();
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }

        // Kullanıcı bir değişiklik yaptığında bayat verileri temizleyen yardımcı metod
        private async Task ClearUserProductsCache()
        {
            int userId = CurrentUserId;
            await _cache.RemoveAsync($"user_{userId}_index_site_all_time_all_page_1");
            await _cache.RemoveAsync($"user_{userId}_approved_page_1");
        }
    }
}
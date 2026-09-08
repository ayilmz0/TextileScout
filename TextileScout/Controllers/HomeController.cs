using Microsoft.AspNetCore.Mvc;
using TextileScout.Web.Data;
using TextileScout.Web.Models;
using TextileScout.Web.Services;

namespace TextileScout.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ScraperService _scraperService;

        public HomeController(AppDbContext context, ScraperService scraperService)
        {
            _context = context;
            _scraperService = scraperService;
        }

        public IActionResult Index()
        {
            // Veritabanındaki ürünleri ekrana gönder
            var products = _context.Products.OrderByDescending(p => p.DetectedAt).ToList();
            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> RunScraper(string targetUrl)
        {
            if (string.IsNullOrEmpty(targetUrl)) return RedirectToAction("Index");

            // 1. Siteyi tara ve resimleri indir
            var scrapedItems = await _scraperService.ScrapeWebsiteAsync(targetUrl);

            // 2. Veritabanına kaydet
            foreach (var item in scrapedItems)
            {
                var newProduct = new Product
                {
                    ImageUrl = item.ImageUrl,
                    LocalImagePath = item.ImageUrl,
                    SourceSite = item.SourceSite,
                    DetectedAt = DateTime.Now,
                    Status = "InReview"
                };
                _context.Products.Add(newProduct);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
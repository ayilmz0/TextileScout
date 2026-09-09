using Microsoft.EntityFrameworkCore;
using TextileScout.Web.Data;
using TextileScout.Web.Models;

namespace TextileScout.Web.Services
{
    public class AutoScraperBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AutoScraperBackgroundService> _logger;

        public AutoScraperBackgroundService(
            IServiceProvider serviceProvider,
            IWebHostEnvironment env,
            ILogger<AutoScraperBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _env = env;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoScraper Arka Plan Servisi Başlatıldı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var scraper = scope.ServiceProvider.GetRequiredService<ScraperService>();
                        var visionApi = scope.ServiceProvider.GetRequiredService<VisionApiService>();

                        var activeSites = await context.TargetSites.Where(s => s.IsActive).ToListAsync();

                        foreach (var site in activeSites)
                        {
                            _logger.LogInformation("{SiteName} taranıyor...", site.Name);
                            var scrapedItems = await scraper.ScrapeWebsiteAsync(site.Url);

                            foreach (var item in scrapedItems)
                            {
                                bool exists = await context.Products.AnyAsync(p => p.ImageUrl == item.ImageUrl);
                                if (exists) continue;

                                var fullPath = Path.Combine(_env.WebRootPath, item.ImageUrl.TrimStart('/'));
                                bool isClothing = await visionApi.IsClothingImageAsync(fullPath);

                                if (isClothing)
                                {
                                    // AutoScraperBackgroundService.cs içinde ürün ekleme bloğu:
                                    context.Products.Add(new Product
                                    {
                                        ImageUrl = item.ImageUrl,
                                        LocalImagePath = item.ImageUrl,
                                        SourceSite = site.Name,
                                        DetectedAt = DateTime.Now,
                                        IsApproved = false,
                                        Status = "InReview",
                                        UserId = site.UserId // SİTENİN SAHİBİ OLAN KULLANICIYA ATANDI
                                    });
                                    _logger.LogInformation("Yeni Kıyafet Eklendi: {SiteName} -> {Url}", site.Name, item.ImageUrl);
                                }
                                else
                                {
                                    if (File.Exists(fullPath)) File.Delete(fullPath);
                                    _logger.LogWarning("Tekstil Dışı Resim Elendi & Silindi: {Url}", item.ImageUrl);
                                }
                            }

                            site.LastScrapedAt = DateTime.Now;
                            await context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Arka plan tarama döngüsünde bir hata oluştu!");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
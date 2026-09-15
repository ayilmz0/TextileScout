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
            _logger.LogInformation("TextileScout Otomatik Tarama Servisi Başlatıldı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var scraper = scope.ServiceProvider.GetRequiredService<ScraperService>();
                        var visionApi = scope.ServiceProvider.GetRequiredService<VisionApiService>();

                        var activeSites = await context.TargetSites.Where(s => s.IsActive).ToListAsync(stoppingToken);

                        foreach (var site in activeSites)
                        {
                            _logger.LogInformation("{SiteName} ({Url}) yeni ürünler için denetleniyor...", site.Name, site.Url);
                            var scrapedItems = await scraper.ScrapeWebsiteAsync(site.Url);

                            int newProductsCount = 0;

                            foreach (var item in scrapedItems)
                            {
                                // KİLİT NOKTA: Bu Orijinal Resim Linki (OriginalUrl) veritabanımızda daha önce var mı?
                                bool exists = await context.Products.AnyAsync(p =>
                                    p.UserId == site.UserId &&
                                    p.ProductUrl == item.OriginalUrl,
                                    stoppingToken);

                                // Eğer bu görsel zaten veritabanında varsa "Eski Ürün"dür, ATLA!
                                if (exists)
                                {
                                    // Temp indirilen dosyayı diskten temizle
                                    var tempPath = Path.Combine(_env.WebRootPath, item.ImageUrl.TrimStart('/'));
                                    if (File.Exists(tempPath)) File.Delete(tempPath);
                                    continue;
                                }

                                // YENİ BİR GÖRSEL BULUNDU! Şimdi Yapay Zeka (YOLOv8) ile analiz et
                                var fullPath = Path.Combine(_env.WebRootPath, item.ImageUrl.TrimStart('/'));
                                bool isClothing = await visionApi.IsClothingImageAsync(fullPath);

                                if (isClothing)
                                {
                                    var newProduct = new Product
                                    {
                                        ImageUrl = item.ImageUrl,
                                        LocalImagePath = item.ImageUrl,
                                        ProductUrl = item.OriginalUrl, // Sitedeki gerçek/orijinal resim linki
                                        SourceSite = site.Name,
                                        DetectedAt = DateTime.Now,
                                        IsApproved = false,
                                        Status = "InReview",
                                        UserId = site.UserId
                                    };

                                    context.Products.Add(newProduct);
                                    await context.SaveChangesAsync(stoppingToken);

                                    newProductsCount++;
                                    _logger.LogInformation("🔥 [YENİ MODEL TESPİT EDİLDİ]: {SiteName} -> {Url}", site.Name, item.OriginalUrl);
                                }
                                else
                                {
                                    if (File.Exists(fullPath)) File.Delete(fullPath);
                                    _logger.LogInformation("Tekstil Dışı/Afiş Resim Elendi: {Url}", item.OriginalUrl);
                                }
                            }

                            site.LastScrapedAt = DateTime.Now;
                            await context.SaveChangesAsync(stoppingToken);

                            if (newProductsCount > 0)
                            {
                                _logger.LogInformation("✅ {SiteName} sitesinden {Count} adet YENİ model yakalandı!", site.Name, newProductsCount);
                            }
                            else
                            {
                                _logger.LogInformation("ℹ️ {SiteName} sitesinde yeni bir model bulunamadı (Tüm ürünler güncel).", site.Name);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Tarama döngüsünde bir hata oluştu!");
                }

                // Taramalar arası bekleme süresi (Örn: 15 dakikada bir kontrol et)
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
}
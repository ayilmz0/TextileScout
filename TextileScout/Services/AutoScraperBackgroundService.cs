using Microsoft.EntityFrameworkCore;
using TextileScout.Web.Data;
using TextileScout.Web.Models;

namespace TextileScout.Web.Services
{
    public class AutoScraperBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public AutoScraperBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Uygulama açık olduğu sürece döngü devam eder
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var scraper = scope.ServiceProvider.GetRequiredService<ScraperService>();

                    // Sadece aktif olan hedef siteleri getir
                    var activeSites = await context.TargetSites.Where(s => s.IsActive).ToListAsync();

                    foreach (var site in activeSites)
                    {
                        var scrapedItems = await scraper.ScrapeWebsiteAsync(site.Url);

                        foreach (var item in scrapedItems)
                        {
                            // MÜKERRER KONTROLÜ: Aynı resim URL'si veritabanında daha önce yoksa YENİ ÜRÜN olarak ekle
                            bool exists = await context.Products.AnyAsync(p => p.ImageUrl == item.ImageUrl);
                            if (!exists)
                            {
                                context.Products.Add(new Product
                                {
                                    ImageUrl = item.ImageUrl,
                                    LocalImagePath = item.ImageUrl,
                                    SourceSite = site.Name,
                                    DetectedAt = DateTime.Now,
                                    Status = "InReview"
                                });
                            }
                        }

                        site.LastScrapedAt = DateTime.Now;
                        await context.SaveChangesAsync();
                    }
                }

                // 1 saat bekle, ardından tekrar tara (Test için zamanı düşürebilirsiniz)
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
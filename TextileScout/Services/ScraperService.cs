using PuppeteerSharp;
using TextileScout.Web.DTOs;

namespace TextileScout.Web.Services
{
    public class ScraperService
    {
        private readonly IWebHostEnvironment _env;

        public ScraperService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<List<ScrapedProductDto>> ScrapeWebsiteAsync(string targetUrl)
        {
            var result = new List<ScrapedProductDto>();

            try
            {
                // Chrome indiricisini yapılandır ve indir
                var fetcher = new BrowserFetcher();
                await fetcher.DownloadAsync();

                // Tarayıcıyı başlat (Kullanıcı gibi görünmesi için UserAgent ve Boyut eklendi)
                await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
                });

                await using var page = await browser.NewPageAsync();
                await page.SetViewportAsync(new ViewPortOptions { Width = 1280, Height = 800 });
                await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                // Sayfaya git
                await page.GoToAsync(targetUrl, new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded },
                    Timeout = 60000
                });

                // Sayfayı biraz aşağı kaydır (Lazy-loading resimlerin yüklenmesi için)
                await page.EvaluateExpressionAsync("window.scrollBy(0, 800)");
                await Task.Delay(2000); // Resimlerin dolmasını bekle

                // Sayfadaki tüm img etiketlerinin src veya data-src adreslerini topla
                var imageLinks = await page.EvaluateExpressionAsync<string[]>(@"
                    Array.from(document.querySelectorAll('img'))
                        .map(img => img.src || img.getAttribute('data-src'))
                        .filter(src => src && src.startsWith('http'))
                ");

                // Kayıt dizini
                var savePath = Path.Combine(_env.WebRootPath, "images", "scraped");
                if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

                // Sayfadan bulunan ilk 5 geçerli resmi indir
                foreach (var imgUrl in imageLinks.Distinct().Take(5))
                {
                    try
                    {
                        var imageBytes = await httpClient.GetByteArrayAsync(imgUrl);

                        // Sadece küçük ikon/logo olmayan gerçek resimleri al (Boyut kontrolü: > 5KB)
                        if (imageBytes.Length < 5120) continue;

                        var fileName = $"{Guid.NewGuid()}.jpg";
                        var filePath = Path.Combine(savePath, fileName);

                        await File.WriteAllBytesAsync(filePath, imageBytes);

                        result.Add(new ScrapedProductDto
                        {
                            SourceUrl = targetUrl,
                            ImageUrl = $"/images/scraped/{fileName}",
                            SourceSite = new Uri(targetUrl).Host
                        });
                    }
                    catch
                    {
                        // İndirilemeyen resimleri sessizce atla
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scraping Hatası: {ex.Message}");
            }

            return result;
        }
    }
}
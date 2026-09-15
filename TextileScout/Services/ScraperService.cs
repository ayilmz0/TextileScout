using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
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

            Console.WriteLine($"\n[DEDEKTİF] 1. {targetUrl} adresine gidiliyor...");

            try
            {
                var fetcher = new BrowserFetcher();
                await fetcher.DownloadAsync();

                await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
                });

                await using var page = await browser.NewPageAsync();
                await page.SetViewportAsync(new ViewPortOptions { Width = 1280, Height = 900 });
                await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                await page.GoToAsync(targetUrl, new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
                    Timeout = 60000
                });

                await page.EvaluateExpressionAsync("window.scrollBy(0, 1000)");
                await Task.Delay(2000);

                // Kategori sayfasındaki doğrudan ÜRÜN GÖRSELLERİNİ topla (Afiş, logo ve tasarımları ele)
                var imageUrls = await page.EvaluateExpressionAsync<string[]>(@"
                    Array.from(document.querySelectorAll('img'))
                        .map(img => img.src || img.getAttribute('data-src') || img.getAttribute('data-original'))
                        .filter(src => src && src.startsWith('http') && 
                            !src.includes('sayfatasarim') && 
                            !src.includes('logo') && 
                            !src.includes('banner') &&
                            !src.includes('icon') &&
                            !src.includes('title'))
                ");

                var targetImages = imageUrls != null ? imageUrls.Distinct().Take(10).ToList() : new List<string>();
                Console.WriteLine($"[DEDEKTİF] 2. Filtreden geçen net ürün görseli sayısı: {targetImages.Count}");

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

                var savePath = Path.Combine(_env.WebRootPath, "images", "scraped");
                if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

                foreach (var imgUrl in targetImages)
                {
                    try
                    {
                        var imageBytes = await httpClient.GetByteArrayAsync(imgUrl);
                        if (imageBytes.Length < 15000) continue; // 15KB altı küçük simge/ikonları atla

                        var fileName = $"{Guid.NewGuid()}.jpg";
                        var filePath = Path.Combine(savePath, fileName);
                        await File.WriteAllBytesAsync(filePath, imageBytes);

                        Console.WriteLine($"[DEDEKTİF] + Ürün Görseli Yakalandı: {fileName}");

                        result.Add(new ScrapedProductDto
                        {
                            SourceUrl = targetUrl,
                            ImageUrl = $"/images/scraped/{fileName}",
                            OriginalUrl = imgUrl,
                            SourceSite = new Uri(targetUrl).Host
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DEDEKTİF] X İndirme Hatası: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEDEKTİF] GENEL HATA: {ex.Message}");
            }

            Console.WriteLine($"[DEDEKTİF] --- TARAMA BİTTİ. Python'a gönderilecek resim sayısı: {result.Count} ---");
            return result;
        }
    }
}
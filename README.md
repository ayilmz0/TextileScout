# 🛰️ TextileScout

> **Tekstil ve Moda Sektörü İçin Otomatik Rakip/Trend İzleme ve Yapay Zeka Destekli Filtreleme Platformu**

TextileScout, tekstil üreticileri ve e-ticaret markaları için geliştirilmiş **otomatik bir görsel istihbarat ve trend izleme sistemidir**. Belirlenen hedef rakip e-ticaret sitelerini arka planda düzenli olarak tarar, yeni eklenen ürün görsellerini toplar ve entegre **YOLOv8 Vision API** servisi ile reklam banner'ı, logo veya tekstil dışı görselleri eleyerek yalnızca gerçek kıyafet/model fotoğraflarını onay panelinize aktarır.

---

## 🛠️ Teknolojik Mimari (Tech Stack)

### **Backend & Core (.NET 10 / ASP.NET Core MVC)**
* **Mimari:** Katmanlı & Modüler Mimari, Clean Code Standartları
* **Veritabanı & ORM:** Entity Framework Core, MS SQL Server
* **Caching & Message Broker:** **Redis** (Cache-Aside Pattern, TTL Yönetimi, Task Queue)
* **Kimlik Doğrulama:** JWT (Access Token) + **Redis-backed Refresh Token** (HttpOnly, Secure Cookie)
* **Güvenlik & Performans:** IP Bazlı Redis Rate Limiting Middleware
* **Web Kazıma:** PuppeteerSharp (Headless Chrome Otomasyonu & Background Worker)
* **Test Altyapısı:** xUnit, FluentAssertions, Moq

### **Görüntü İşleme & AI (Python)**
* **Framework:** FastAPI
* **Computer Vision:** YOLOv8 (Ultralytics) - Tekstil / Kıyafet Nesne Tespiti
* **Kuyruk Entegrasyonu:** Redis Queue (`BRPOP`) Asenkron Görsel İşleme

### **DevOps & Konteynerizasyon**
* **Containerization:** **Docker & Docker Desktop** (Redis Container Entegrasyonu)

### **Frontend / Arayüz**
* **Tasarım:** Özel SaaS/Dashboard Arayüzü (Inter / Plus Jakarta Sans Tipografisi)
* **Bileşenler:** Bootstrap 5, Bootstrap Icons, Responsive Grid Yapısı

---

## 🚀 Öne Çıkan Özellikler

- 🤖 **Tam Otomatik Arka Plan Taraması (Worker Service):** Kullanıcı müdahalesi gerektirmeden, tanımlanan hedef siteleri arka planda periyodik olarak tarar.
- ⚡ **Redis Önbellekleme Katmanı:** Ürün listeleri, filtreleme ve sayfalama sonuçları Redis RAM önbelleğinde saklanır; yanıt süreleri 1-2 ms seviyesine düşürülür.
- 🎯 **Yapay Zeka Destekli Görsel Filtreleme:** Web sitelerindeki gereksiz logolar, kampanya banner'ları ve tekstil dışı görseller YOLOv8 modeli ile elenir.
- 🛡️ **Gelişmiş Güvenlik & Rate Limiter:** Kötü amaçlı botlara ve brute-force saldırılarına karşı IP bazlı Redis Rate Limiting koruması.
- 🔑 **Güvenli JWT & Redis Refresh Token:** 15 dakikalık kısa ömürlü Access Token ve Redis üzerinde 7 gün saklanan, oturum kapatıldığında anında iptal edilen (Revoke) Refresh Token yapısı.
- 📡 **Asenkron İş Kuyruğu (Redis Task Queue):** Web scraper tarafından yakalanan görseller Redis kuyruğuna atanır ve Python AI servisi tarafından kilitlenme olmadan asenkron işlenir.
- 🔒 **Güvenli Yönetici Paneli:** Cookie tabanlı JWT kimlik doğrulama ile tam koruma.
- 👆 **Kolay Operasyon Paneli:** Tek tıkla görselleri **Onayla (Üretime Al)** veya **Sil** yapabileceğiniz minimalist yönetim ekranı.
- 🌐 **Dinamik Hedef Site Yönetimi (CRUD):** Kod değiştirmeye gerek kalmadan panel üzerinden yeni izlenecek rakip/marka sitelerinin eklenebilmesi.

---

## 📁 Katmanlı Proje Yapısı

```text
TextileScout.Web/
├── Controllers/       # HTTP İsteklerini karşılayan ve Views/JSON dönen yönlendiriciler
├── Data/              # AppDbContext ve EF Core Veritabanı konfigürasyonları
├── DTOs/              # Veri Taşıma Nesneleri & ViewModel Yapıları
├── Logs/              # Sistem Hata ve Çalışma Logları
├── Middlewares/       # RateLimitationMiddleware vb. güvenlik katmanları
├── Migrations/        # EF Core Veritabanı Şema Takip Dosyaları
├── Models/            # Veritabanı Tablo Karşılıkları (User, Product, TargetSite)
├── Services/          # TokenService (JWT/Redis), RedisQueueService, Scraper
└── Views/             # Razor Arayüz Tasarımları (.cshtml)

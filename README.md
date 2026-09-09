# 🛰️ ModaRadar (TextileScout)

> **Tekstil ve Moda Sektörü İçin Otomatik Rakip/Trend İzleme ve Yapay Zeka Destekli Filtreleme Platformu**

ModaRadar, tekstil üreticileri ve e-ticaret markaları için geliştirilmiş **otomatik bir görsel istihbarat sistemidir**. Belirlenen hedef rakip siteleri arka planda düzenli olarak tarar, yeni eklenen ürün görsellerini toplar ve entegre **YOLOv8 Vision API** servisi ile reklam banner'ı, logo veya tekstil dışı görselleri eleyerek yalnızca gerçek kıyafet/model fotoğraflarını onay panelinize aktarır.

---

## 🛠️ Teknolojik Mimari (Tech Stack)

### **Backend (.NET 9 / ASP.NET Core MVC)**
* **Mimari:** Katmanlı Mimari, Arka Plan Servis (Worker) Mimarisi
* **Veritabanı & ORM:** Entity Framework Core, MS SQL Server
* **Kimlik Doğrulama:** JWT + HttpOnly Cookie-based Authentication
* **Web Kazıma:** PuppeteerSharp (Headless Chrome Otomasyonu)
* **Entegrasyon:** VisionApiService (RESTful Servisler Arası İletişim)

### **Görüntü İşleme & AI (Python)**
* **Framework:** FastAPI
* **Computer Vision:** YOLOv8 (Ultralytics) - Tekstil / Kıyafet Nesne Tespiti

### **Frontend / Arayüz**
* **Tasarım:** Özel SaaS/Dashboard Arayüzü (Inter / Plus Jakarta Sans Tipografisi)
* **Bileşenler:** Bootstrap 5, Bootstrap Icons, Responsive Grid Yapısı

---

## 🚀 Öne Çıkan Özellikler

- 🤖 **Tam Otomatik Arka Plan Taraması (Worker Service):** Kullanıcı müdahalesi gerektirmeden, tanımlanan hedef siteleri arka planda periyodik olarak tarar.
- 🎯 **Yapay Zeka Destekli Görsel Filtreleme:** Web sitelerindeki gereksiz logolar, kampanya banner'ları ve tekstil dışı görseller Yapay Zeka modeli ile elenir.
- 🔒 **Güvenli Yönetici Paneli:** JWT ve Cookie tabanlı kimlik doğrulama ile tam koruma.
- ⚡ **Kolay Operasyon Paneli:** Tek tıkla görselleri **Beğen (Üretime Al)** veya **Sil** yapabileceğiniz minimalist yönetim ekranı.
- 🌐 **Dinamik Hedef Site Yönetimi (CRUD):** Kod değiştirmeye gerek kalmadan panel üzerinden yeni izlenecek rakip/marka sitelerinin eklenebilmesi.

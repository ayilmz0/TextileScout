namespace TextileScout.Web.Models
{
    public class TargetSite
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Örn: "Rakip X Mağazası"
        public string Url { get; set; } = string.Empty;  // Örn: "https://site.com/yeni-gelenler"
        public bool IsActive { get; set; } = true;
        public DateTime? LastScrapedAt { get; set; }
    }
}
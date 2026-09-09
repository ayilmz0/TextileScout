namespace TextileScout.Web.Models
{
    public class TargetSite
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime? LastScrapedAt { get; set; }

        // MÜLKİYET İLİŞKİSİ
        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
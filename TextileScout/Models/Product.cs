namespace TextileScout.Web.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string LocalImagePath { get; set; } = string.Empty;
        public string SourceSite { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; } = DateTime.Now;
        public bool IsApproved { get; set; } = false;
        public string Status { get; set; } = "InReview";

        // MÜLKİYET İLİŞKİSİ
        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
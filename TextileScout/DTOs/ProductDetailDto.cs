namespace TextileScout.Web.DTOs
{
    public class ProductDetailDto
    {
        public int Id { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string SourceSite { get; set; } = string.Empty;
        public string FormattedDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
    }
}
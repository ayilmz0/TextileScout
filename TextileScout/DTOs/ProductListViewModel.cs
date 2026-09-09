using TextileScout.Web.Models;

namespace TextileScout.Web.DTOs
{
    public class ProductListViewModel
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();

        // Filtre parametreleri
        public string? SelectedSite { get; set; }
        public string? TimeRange { get; set; } // "24h", "7d", "all"
        public List<string> AvailableSites { get; set; } = new List<string>();

        // Sayfalama parametreleri
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 12; // Sayfa başı 12 ürün
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
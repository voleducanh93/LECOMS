using LECOMS.Data.DTOs.Product;

namespace LECOMS.Data.DTOs.Recombee
{
    public class BrowseResultDTO
    {
        public IEnumerable<ProductDTO> RecommendedProducts { get; set; }
        public IEnumerable<object> RecommendedCategories { get; set; }
        public IEnumerable<ProductDTO> BestSellerProducts { get; set; }
    }
}

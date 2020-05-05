using WebScraper.Models;

namespace WebScraper.DTO
{
    public class ParamsDTO : ProductModel
    {
        public string HomeUrl { get; set; }
        public string ProductList { get; set; }
    }
}

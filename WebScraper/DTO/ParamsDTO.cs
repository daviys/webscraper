using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebScraper.DTO
{
    public class ParamsDTO
    {
        public string HomeUrl { get; set; }
        public string ProductList { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public string Price { get; set; }
    }
}

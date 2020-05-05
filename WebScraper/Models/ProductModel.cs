using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebScraper.Models
{
    public class ProductModel
    {
        /* Основные параметры */

        [Display(Name = "ID")]
        public int Id { get; set; }
        [Display(Name = "Ссылка на товар")]
        public string ItemLink { get; set; }
        [Display(Name = "Наименование")]
        public string Name { get; set; }
        [Display(Name = "Цена")]
        public string Price { get; set; }
        [Display(Name = "Описание товара (html)")]
        public string Description { get; set; }
        [Display(Name = "Ссылка на изображение")]
        public string ImgHref { get; set; }

        /* Дополнительные параметры */

        [Display(Name = "Бренд")]
        public string Brand { get; set; }
        [Display(Name = "Категория")]
        public string Category { get; set; }
        [Display(Name = "Описание товара текстом")]
        public string DescriptionText { get; set; }
        [Display(Name = "Старая цена")]
        public string OldPrice { get; set; }
        //public enum Level { get; set; }
    }
}

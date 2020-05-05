using HtmlAgilityPack;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using WebScraper.DTO;
using WebScraper.Hubs;
using WebScraper.Infrastructure;
using WebScraper.Models;

namespace WebScraper.Services
{
    public class ScrapingService : IScrapingService
    {
        #region Fields

        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IHubContext<ScraperHub> _hubContext;

        public string sitename = "";
        public string lastLink = "";
        public string filename = "";

        public int level = 0; // уровни вложенности, прохода
        public int productCountSuccess = 0;
        public int productCountError = 0;

        #endregion

        #region Ctor

        public ScrapingService(
            IWebHostEnvironment environment,
            IHubContext<ScraperHub> hubContext
            )
        {
            _hostingEnvironment = environment;
            _hubContext = hubContext;
        }
        
        #endregion Ctor


        public async Task StartScraping(ParamsDTO queryParams)
        {
            try
            {
                HtmlWeb web = new HtmlWeb();

                var htmlDoc = web.Load(queryParams.HomeUrl);

                var products = htmlDoc.DocumentNode.SelectNodes(queryParams.ProductList);

                level++;

                var links = products.Descendants("a").Select(a => a.Attributes["href"].Value).ToList();

                if (level == 1)
                {
                    lastLink = links.Last();
                    sitename = UrlConverter.GetSiteHostWithProtocol(queryParams.HomeUrl);
                    SetFilename();
                }

                await ScrapProducts(links, queryParams);
            }
            catch (Exception ex)
            {
                await _hubContext.Clients.All.SendAsync("Send", $"Error due parsing website or product list");
                throw (ex);
            }
        }

        private async Task ScrapProducts(List<string> links, ParamsDTO queryParams)
        {
            if (links != null)
            {
                foreach (var url in links)
                {
                    var item = GetProduct(url, queryParams);
                    if (item.Result != null)
                    {
                        if (!item.Result.Name.StartsWith("---"))
                            await _hubContext.Clients.All.SendAsync("Send", $"{++productCountSuccess} {item.Result.Name} {item.Result.Price} {item.Result.Description} {item.Result.ImgHref}");
                        if (url == lastLink)
                            await _hubContext.Clients.All.SendAsync("Send", $"Количество спарсенных {productCountSuccess}, ошибок {productCountError}");

                        System.IO.File.AppendAllText(filename, $"{productCountSuccess};{item.Result.ItemLink};{item.Result.Name};{item.Result.Price};{item.Result.Description};{item.Result.ImgHref};" +
                            $"{item.Result.Brand};{item.Result.Category};{item.Result.DescriptionText};{item.Result.OldPrice}" + Environment.NewLine, Encoding.GetEncoding("utf-8"));
                    }
                    else
                    {
                        await _hubContext.Clients.All.SendAsync("Send", $"Error due parsing product");
                        ++productCountError;
                    }
                }
            }
            else
            {
                await _hubContext.Clients.All.SendAsync("Send", $"Error due parsing product list");
            }
        }

        private async Task<ProductModel> GetProduct(string url, ParamsDTO queryParams)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));

            HtmlWeb web = new HtmlWeb();

            var htmlDoc = web.Load(sitename + url);

            var product = htmlDoc.DocumentNode.SelectSingleNode(queryParams.Name) != null
                          || htmlDoc.DocumentNode.SelectSingleNode(queryParams.Description) != null;

            if (product)
            {
                var img = htmlDoc.DocumentNode.SelectSingleNode(queryParams.ImgHref);

                return new ProductModel()
                {
                    ItemLink = url,
                    Name = htmlDoc.DocumentNode.SelectSingleNode(queryParams.Name) != null ? htmlDoc.DocumentNode.SelectSingleNode(queryParams.Name).InnerText : null,
                    Description = htmlDoc.DocumentNode.SelectSingleNode(queryParams.Description) != null ? Regex.Replace(htmlDoc.DocumentNode.SelectSingleNode(queryParams.Description).InnerHtml, @"\t|\n|\r", "").Replace("  ", " ") : null,
                    Price = htmlDoc.DocumentNode.SelectSingleNode(queryParams.Price) != null ? HttpUtility.HtmlDecode(htmlDoc.DocumentNode.SelectSingleNode(queryParams.Price).InnerHtml).Replace("  ", " ") : null,
                    ImgHref = img != null ? sitename + string.Join("," + sitename, img.Descendants("img").Select(z => z.Attributes["src"].Value).ToList()) : string.Empty,
                    Brand = (queryParams.Brand != null && htmlDoc.DocumentNode.SelectSingleNode(queryParams.Brand) != null) ? htmlDoc.DocumentNode.SelectSingleNode(queryParams.Brand).InnerText : null,
                    Category = (queryParams.Category != null && htmlDoc.DocumentNode.SelectSingleNode(queryParams.Category) != null) ? htmlDoc.DocumentNode.SelectSingleNode(queryParams.Category).InnerText : null,
                    DescriptionText = (queryParams.DescriptionText != null && htmlDoc.DocumentNode.SelectSingleNode(queryParams.DescriptionText) != null) ? Regex.Replace(htmlDoc.DocumentNode.SelectSingleNode(queryParams.DescriptionText).InnerText, @"\t|\n|\r", "").Replace("  ", " ") : null,
                    OldPrice = (queryParams.OldPrice != null && htmlDoc.DocumentNode.SelectSingleNode(queryParams.OldPrice) != null) ? HttpUtility.HtmlDecode(htmlDoc.DocumentNode.SelectSingleNode(queryParams.OldPrice).InnerHtml).Replace("  ", " ") : null
                };
            }
            else
            {
                var isProductListPage = htmlDoc.DocumentNode.SelectNodes(queryParams.ProductList);
                // проверка если мы на странице продуктов, то снова парсим
                if (isProductListPage != null)
                {
                    var model = queryParams;
                    model.HomeUrl = queryParams.HomeUrl + url;
                    await StartScraping(model);
                    // возвращаем модель без данных, чтобы не возвращался null
                    return new ProductModel() { Name = "---" };
                }
            }

            return null;
        }

        private void SetFilename()
        {
            string folder = _hostingEnvironment.ContentRootPath + "\\AppData\\" + DateTime.Now.ToString("dd.MM.yyyy");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var date = DateTime.Now.ToString("dd.MM.yyyy-HH-mm");
            filename = Path.Combine(folder, "brandname-" + date + ".csv"); // brandname to variable
            var fileHeaders = typeof(ProductModel)
                                .GetProperties()
                                .Select(x => x.GetCustomAttribute<DisplayAttribute>())
                                .Where(x => x != null)
                                .Select(x => x.Name);

            // set headers
            if (!System.IO.File.Exists(filename))
            {
                System.IO.File.WriteAllText(filename, $"{string.Join(";", fileHeaders)}" + Environment.NewLine, Encoding.GetEncoding("utf-8"));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WebScraper.DTO;
using WebScraper.Hubs;
using WebScraper.Models;

namespace WebScraper.Controllers
{
    public class HomeController : Controller
    {
        #region Fields

        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IHubContext<ScraperHub> _hubContext;
        private readonly ILogger<HomeController> _logger;

        public string lastLink = "";
        
        public int level = 0; // уровни вложенности, прохода
        public int productCountSuccess = 0;
        public int productCountError = 0;

        #endregion

        #region Ctor
        public HomeController(
            IWebHostEnvironment environment,
            IHubContext<ScraperHub> hubContext,
            ILogger<HomeController> logger
            )
        {
            _hostingEnvironment = environment;
            _hubContext = hubContext;
            _logger = logger;
        }

        #endregion

        #region Actions

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Scraper()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        #endregion

        #region Methods

        [HttpPost]
        public async Task StartParcing(ParamsDTO dtoParams)
        {
            try
            {
                HtmlWeb web = new HtmlWeb();

                var htmlDoc = web.Load(dtoParams.HomeUrl);

                var products = htmlDoc.DocumentNode.SelectNodes("//*[@class='"+ dtoParams.ProductList +"']");

                level++;

                var links = products.Descendants("a").Select(a => a.Attributes["href"].Value).ToList();

                if (level == 1)
                    lastLink = links.Last();

                await DisplayProducts(links, dtoParams);
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        private async Task DisplayProducts(List<string> links, ParamsDTO dtoParams)
        {
            if (links != null)
            {
                foreach (var url in links)
                {
                    var item = DoWork(url, dtoParams);
                    if (item.Result != null)
                    {
                        if (!item.Result.Name.StartsWith("---"))
                            await _hubContext.Clients.All.SendAsync("Send", $"{++productCountSuccess} {item.Result.Name} {item.Result.Price} {item.Result.Description} {item.Result.ImgHref}");
                        if (url == lastLink)
                            await _hubContext.Clients.All.SendAsync("Send", $"Количество спарсенных {productCountSuccess}, ошибок {productCountError}");
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

        private async Task<ProductModel> DoWork(string url, ParamsDTO dtoParams)
        {
            HtmlWeb web = new HtmlWeb();

            var sitename = GetSiteHostWithProtocol(dtoParams.HomeUrl);
            var htmlDoc = web.Load(sitename + url);

            var product = htmlDoc.DocumentNode.SelectSingleNode("//*[@class='" + dtoParams.Description + "']");

            if (product != null)
            {
                var img = htmlDoc.DocumentNode.SelectSingleNode("//*[@class='" + dtoParams.Image + "']");

                return new ProductModel()
                {
                    Name = htmlDoc.DocumentNode.SelectSingleNode("//*[@class='" + dtoParams.Name + "']") != null ? htmlDoc.DocumentNode.SelectSingleNode("//*[@class='" + dtoParams.Name + "']").InnerText : null,
                    Description = htmlDoc.DocumentNode.SelectSingleNode("//*[@class='" + dtoParams.Description + "']") != null ? Regex.Replace(htmlDoc.DocumentNode.SelectSingleNode("//*[@class='" + dtoParams.Description + "']").InnerHtml, @"\t|\n|\r", "").Replace("  ", " ") : null,
                    Price = htmlDoc.DocumentNode.SelectSingleNode("//*[@class='" + dtoParams.Price + "']") != null ? HttpUtility.HtmlDecode(htmlDoc.DocumentNode.SelectSingleNode("//*[@class='" + dtoParams.Price + "']").InnerHtml).Replace("  ", " ") : null,
                    ImgHref = img != null ? sitename + string.Join("," + sitename, img.Descendants("img").Select(z => z.Attributes["src"].Value).ToList()) : string.Empty
                };
            }
            else
            {
                var isProductListPage = htmlDoc.DocumentNode.SelectNodes("//*[@class='" + dtoParams.ProductList + "']");
                // проверка если мы на странице продуктов, то снова парсим
                if (isProductListPage != null)
                {
                    var model = dtoParams;
                    model.HomeUrl = dtoParams.HomeUrl + url;
                    await StartParcing(model);
                    // возвращаем модель без данных, чтобы не возвращался null
                    return new ProductModel() { Name = "---" };
                }
            }
            
            return null;
        }

        // util; return https://sitename.com
        private string GetSiteHostWithProtocol(string url)
        {
            Uri uri = new Uri(url);
            return uri.Scheme + Uri.SchemeDelimiter + uri.Host;
        }

        #endregion Methods
    }
}

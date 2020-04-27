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

        public async Task StartParcing(string homeUrl)
        {
            try
            {
                HtmlWeb web = new HtmlWeb();

                var htmlDoc = web.Load(homeUrl);

                var products = htmlDoc.DocumentNode.SelectNodes("//ul[@class='list']");

                //ViewBag.Html = GetLinksFromPage(htmlDoc);
                //ViewBag.Html = products.Descendants("a").Select(a => a.Attributes["href"].Value).ToList();

                //string folder = _hostingEnvironment.ContentRootPath + "\\AppData\\" + DateTime.Now.ToString("dd.MM.yyyy");
                //if (!Directory.Exists(folder))
                //{
                //    Directory.CreateDirectory(folder);
                //}

                //var date = DateTime.Now.ToString("dd.MM.yyyy-HH-mm");
                //string filePath = Path.Combine(folder, "brandname-" + date + ".csv"); // brandname to variable
                //var fileHeaders = typeof(ItemModel)
                //                    .GetProperties()
                //                    .Select(x => x.GetCustomAttribute<DisplayAttribute>())
                //                    .Where(x => x != null)
                //                    .Select(x => x.Name);

                var links = products.Descendants("a").Select(a => a.Attributes["href"].Value).ToList();

                await DisplayProducts(links, homeUrl);

                //if (System.IO.File.Exists(filePath))
                //{
                //    System.IO.File.Delete(filePath);
                //}
                // BLL
                //using (StreamWriter streamWriter = new StreamWriter(filePath, true, Encoding.GetEncoding("utf-8")))
                //{
                // set headers
                //streamWriter.WriteLine($"{string.Join(";", fileHeaders)}");

                //int i = 1;

                // think
                //if (links != null)
                //{
                //    foreach (var url in links)
                //    {
                //        var item = DoWork(url);
                //        if (item != null)
                //            streamWriter.WriteLine($"{i++};{item.Name};{item.Price};{item.Description}");
                //    }
                //}
                //}
            }
            catch (Exception ex)
            {
                // refatctor this
                throw (ex);
            }
        }

        private async Task DisplayProducts(List<string> links, string homeUrl)
        {
            if (links != null)
            {
                foreach (var url in links)
                {
                    var item = DoWork(url, homeUrl);
                    if (item.Result != null)
                    {
                        await _hubContext.Clients.All.SendAsync("Send", $"{++productCountSuccess} {item.Result.Name} {item.Result.Price} {item.Result.Description} {item.Result.ImgHref}");
                    }
                    else
                    {
                        await _hubContext.Clients.All.SendAsync("Send", $"Error due parsing product");
                        productCountError++;
                    }
                }
            }
            else
            {
                await _hubContext.Clients.All.SendAsync("Send", $"Error due parsing product list");
            }
        }

        private async Task<ProductModel> DoWork(string url, string homeUrl)
        {
            HtmlWeb web = new HtmlWeb();

            var sitename = GetSiteHostWithProtocol(homeUrl);
            var htmlDoc = web.Load(sitename + url);

            var product = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='parts']");

            if (product != null)
            {
                var img = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='tpl-variable-part']");

                return new ProductModel()
                {
                    Name = htmlDoc.DocumentNode.SelectSingleNode("/html/body/div[2]/div/div[2]/div[2]/h4") != null ? htmlDoc.DocumentNode.SelectSingleNode("/html/body/div[2]/div/div[2]/div[2]/h4").InnerText : null,
                    Description = htmlDoc.DocumentNode.SelectSingleNode("/html/body/div[2]/div/div[2]/div[2]/div") != null ? Regex.Replace(htmlDoc.DocumentNode.SelectSingleNode("/html/body/div[2]/div/div[2]/div[2]/div").InnerHtml, @"\t|\n|\r", "").Replace("  ", " ") : null,
                    Price = htmlDoc.DocumentNode.SelectSingleNode("/html/body/div[2]/div/div[2]/div[2]/form/p/span[1]") != null ? HttpUtility.HtmlDecode(htmlDoc.DocumentNode.SelectSingleNode("/html/body/div[2]/div/div[2]/div[2]/form/p/span[1]").InnerHtml).Replace("  ", " ") : null,
                    ImgHref = img != null ? sitename + string.Join("," + sitename, img.Descendants("img").Select(z => z.Attributes["src"].Value).ToList()) : string.Empty
                };
            }

            await StartParcing(homeUrl + url);
            return null;
        }

        private List<string> GetLinksFromPage(HtmlDocument doc)
        {
            var res = new List<string>();

            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
            {
                HtmlAttribute att = link.Attributes["href"];

                if (att.Value.Contains("a"))
                {
                    res.Add(att.Value);
                }
            }

            return res;
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

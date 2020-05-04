using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebScraper.DTO;
using WebScraper.Models;
using WebScraper.Services;

namespace WebScraper.Controllers
{
    public class HomeController : Controller
    {
        #region Fields

        private readonly IScrapingService _scraper;
        private readonly ILogger<HomeController> _logger;

        #endregion

        #region Ctor
        public HomeController(
            IScrapingService scraper,
            ILogger<HomeController> logger
            )
        {
            _scraper = scraper;
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
        
        public void StartScraping(ParamsDTO queryParams)
        {
            _scraper.StartScraping(queryParams);
        }

        #endregion Methods
    }
}

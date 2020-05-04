using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebScraper.DTO;
using WebScraper.Models;

namespace WebScraper.Services
{
    public interface IScrapingService
    {
        Task StartScraping(ParamsDTO queryParams);
    }
}

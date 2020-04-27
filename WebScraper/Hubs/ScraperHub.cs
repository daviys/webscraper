using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace WebScraper.Hubs
{
    public class ScraperHub : Hub
    {
        public async Task Send(string message)
        {
            try
            {
                await this.Clients.All.SendAsync("Send", message);
            }
            catch (Exception ex)
            {
                // refatctor this
                throw (ex);
            }
        }
    }
}

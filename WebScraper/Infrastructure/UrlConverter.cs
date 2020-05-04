using System;

namespace WebScraper.Infrastructure
{
    public static class UrlConverter
    {
        public static string GetSiteHostWithProtocol(string url)
        {
            Uri uri = new Uri(url);
            return uri.Scheme + Uri.SchemeDelimiter + uri.Host;
        }
    }
}

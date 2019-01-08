using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using RobotsTxt;

namespace Crawler
{
    class Program
    {
        static void Main(string[] args)
        {
            Crawler cr = new Crawler(
            new string[]
            {   // Seed URLs
                "https://docs.python.org/",
                "https://msdn.microsoft.com/",
                "https://devdocs.io/",
                "https://docs.python-guide.org/"
            },

            new string[]
            {   // Prioritized hosts
                "https://docs.python.org/",
                "https://msdn.microsoft.com/",
                "https://devdocs.io/",
                "https://docs.python-guide.org/"
            },
            
            new string[] 
            {   // Prioritized keywords
                "python"
            });

            cr.Crawl();
        }
    }
}

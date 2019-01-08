using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Crawler
{
    class CrawlerContext : DbContext
    {
        public DbSet<WebPage> WebPages { get; set; }
    }
}

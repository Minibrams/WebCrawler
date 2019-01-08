using System;
using System.Collections.Generic;
using System.Text;

namespace Crawler
{
    class WebPage
    {
        public int Id { get; set; }
        public Uri Uri { get; set; }
        public string Contents { get; set; }
        public WebPage(Uri uri, string contents)
        {
            Uri = uri;
            Contents = contents;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using RobotsTxt;
using HtmlAgilityPack;

namespace Crawler
{
    class AccessManager
    {
        Dictionary<string, Robots> HostToRobotsMap;

        public AccessManager()
        {
            HostToRobotsMap = new Dictionary<string, Robots>();
        }

        public bool CanAccess(Uri uri)
        {
            string host = uri.Host;
            if (HostToRobotsMap.ContainsKey(host))
            {
                return HostToRobotsMap[host].IsPathAllowed("*", uri.AbsoluteUri);
            }
            else
            {
                // We haven't seen this website before. 
                // Download its robots.txt and add it to 
                // the map. 
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                {
                    string robots = new HtmlWeb().LoadFromWebAsync($"{uri.Scheme}://{host}/robots.txt")
                    .GetAwaiter()
                    .GetResult().ParsedText;
                    HostToRobotsMap.Add(host, new Robots(robots));
                    return HostToRobotsMap[host].IsPathAllowed("*", uri.AbsoluteUri);
                }
                else
                {
                    return false;
                }
            }
        }
    }
}

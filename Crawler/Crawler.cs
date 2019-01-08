using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using HtmlAgilityPack;
using System.IO;

namespace Crawler
{
    class Crawler
    {
        public Frontier Frontier;
        public AccessManager AccessManager;
        public DuplicateDetector DuplicateDetector;

        public List<WebPage> WebPages;
        public List<string> VisitedPages;

        public Crawler(string[] seedUrls, string[] prioritizedUrls, string[] prioritizedKeywords)
        {
            // Set up the frontier
            Frontier = new Frontier(10);
            Frontier.PrioritizeHosts(prioritizedUrls);
            Frontier.PrioritizeKeyWords(prioritizedKeywords);

            // Set up access manager
            AccessManager = new AccessManager();

            // Duplicate detector
            DuplicateDetector = new DuplicateDetector();

            // Convert all links to URIs 
            List<Uri> URIs = seedUrls.Select(link => new Uri(link)).ToList();

            // Initialize the AccessManager with robots from the seed URIs
            URIs.ForEach(uri => AccessManager.CanAccess(uri));

            // Enqueue all the URIs in the frontier
            URIs.ForEach(uri => Frontier.Enqueue(uri));

            WebPages = new List<WebPage>();
            VisitedPages = new List<string>();
        }

        public void Crawl()
        {
            int numSaves = 0;
            int saveCounter = 0;
            while (true)
            {
                try
                {
                    Frontier.RefillBackQueues();
                    CrawlNext();
                }
                catch (InvalidOperationException e)
                {
                    Console.WriteLine($"Queues are empty, terminating crawl.");
                    return;
                }

                catch (Exception e)
                {
                    Console.WriteLine($"Exception encountered: {e.Message}");
                }

                if (saveCounter > 100)
                {
                    foreach (WebPage webPage in WebPages)
                    {
                        SaveWebPage($"{numSaves}.txt", webPage.Uri, webPage);
                        numSaves++;
                    }

                    WebPages.Clear();
                    saveCounter = 0;
                }

                saveCounter++;
                if (numSaves > 1000)
                    break;
            }
            
        }

        public void CrawlNext()
        {
            Uri uri = Frontier.Dequeue();

            // Info print
            PrintDebugInfo(uri);

            // We can access it because it is already in the
            // frontier. Download the HTML, store it somewhere. 
            // Find all the links, add to frontier if we can access.

            HtmlNodeCollection anchors = null;
            IEnumerable<string> links = null;

            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            {
                HtmlDocument document = new HtmlWeb().LoadFromWebAsync(uri.AbsoluteUri).GetAwaiter().GetResult();
                anchors = document.DocumentNode
                    .SelectNodes("//a[@href]");

                if (document != null)
                {
                    // Check for duplicate, if not duplicate add to WebPages
                    if (!DuplicateDetector.IsDuplicate(document.ParsedText))
                    {
                        WebPages.Add(new WebPage(uri, document.ParsedText));
                    }
                    else
                    {
                        Console.WriteLine($"Found a duplicate! ");
                        return;
                    }
                }
            }
            else
            {
                return;
            }
            
            if (anchors == null)
                return;
            else
                links = anchors.Select(anchor => anchor.Attributes["href"].Value);

            // Extract links, check for relative links and construct correct ones
            List<Uri> extractedURIs = new List<Uri>();
            foreach (var link in links)
            {
                try
                {
                    Uri newUri = null;

                    if (!IsAbsoluteUrl(link))
                    {
                        Uri baseUri = new Uri($"{uri.Scheme}://{uri.Host}");
                        newUri = new Uri(baseUri, link);
                    }
                    else
                    {
                        newUri = new Uri(link);
                    }

                    extractedURIs.Add(newUri);
                }
                catch (UriFormatException e)
                {
                    Console.WriteLine($"Couldn't parse {link}. Continuing...");
                }
                
            }

            // For every newly found URI, add it to the frontier if we can access it 
            // given the robots.txt
            foreach (Uri extractedURI in extractedURIs)
            {
                if (AccessManager.CanAccess(extractedURI) && !VisitedPages.Contains(extractedURI.AbsoluteUri))
                {
                    Frontier.Enqueue(extractedURI);
                    VisitedPages.Add(extractedURI.AbsoluteUri);
                }
            }
        }

        private bool IsAbsoluteUrl(string url)
        {
            Uri result;
            return Uri.TryCreate(url, UriKind.Absolute, out result);
        }

        private string Spacing(int space)
        {
            string ret = "";
            for (int i = 0; i < space; i++)
                ret += " ";
            return ret;
        }

        private void PrintDebugInfo(Uri uri)
        {
            Console.WriteLine($"------------- INFO -------------");
            Console.WriteLine($"{Spacing(20)} ({uri.AbsoluteUri})");
            Console.WriteLine($"Front queues: ");
            Console.WriteLine($"    Unprioritized: {Frontier.FrontQueues[0].Count} URLs.");
            Console.WriteLine($"    Prioritized:   {Frontier.FrontQueues[1].Count} URLs.");
            Console.WriteLine();
            Console.WriteLine($"Back queues: ");
            foreach (TimedQueue<Uri> queue in Frontier.BackQueues)
            {
                string host = $"    ({Frontier.HostToBackQueueMap.FirstOrDefault(x => x.Value == queue).Key})";
                string spacing = Spacing(40 - host.Length);
                Console.WriteLine($"{host}{spacing} : {queue.Count} URLs. ");
            }
            Console.WriteLine($"");
        }

        private void SaveWebPage(string filename, Uri uri, WebPage page)
        {
            string filepath = $"{Directory.GetCurrentDirectory()}\\pages\\{filename}";
            FileStream file = File.Create(filepath);
            StreamWriter writer = new StreamWriter(file);
            writer.WriteLine(uri.Host);
            writer.WriteLine(uri.AbsoluteUri);
            writer.Write(page.Contents);
        }
    }
}

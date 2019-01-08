using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;

namespace Crawler
{
    class Frontier
    {
        public List<Queue<Uri>> FrontQueues { get; set; }

        public List<TimedQueue<Uri>> BackQueues { get; set; }

        public Dictionary<string, TimedQueue<Uri>> HostToBackQueueMap;

        private List<string> _prioritizedHosts;
        private List<string> _prioritizedKeywords;

        public Frontier(int numBackQueues)
        {
            FrontQueues = new List<Queue<Uri>>() { new Queue<Uri>(), new Queue<Uri>()};
            BackQueues = new List<TimedQueue<Uri>>();

            _prioritizedHosts = new List<string>();
            _prioritizedKeywords = new List<string>();
            HostToBackQueueMap = new Dictionary<string, TimedQueue<Uri>>();

            for (int i = 0; i < numBackQueues; i++)
                BackQueues.Add(new TimedQueue<Uri>());
        }

        public void PrioritizeHosts(string[] hosts)
        {
            _prioritizedHosts.AddRange(hosts);
        }

        public void PrioritizeKeyWords(string[] keywords)
        {
            _prioritizedKeywords.AddRange(keywords);
        }

        public void Enqueue(Uri uri)
        {
            if (_prioritizedHosts.Contains(uri.Host) || _prioritizedKeywords.Any(word => uri.AbsoluteUri.Contains(word)))
                FrontQueues[1].Enqueue(uri);
            else
                FrontQueues[0].Enqueue(uri);
        }

        public Uri Dequeue()
        {
            // Check the back queues, find the one with the least time before ping is allowed
            BackQueues.Sort((x, y) => x.CanBeAccessedAt > y.CanBeAccessedAt ? 1 : x.CanBeAccessedAt == y.CanBeAccessedAt ? 0 : -1);
            TimedQueue<Uri> chosenQueue = BackQueues[0];

            // Check if the chosen queue is empty. 
            if (chosenQueue.Count == 0)
            {
                // First, this queue should no longer be in the mapping table
                var mapEntry = HostToBackQueueMap.FirstOrDefault(x => x.Value == chosenQueue);
                if (mapEntry.Key != null)
                    HostToBackQueueMap.Remove(mapEntry.Key);

                bool foundNewHost = false;

                // Back queues can never be empty - keep taking new URIs from the frontier 
                // and add them to their respective back queues. Once we find a new host, 
                // map that host to this queue. 

                while (!foundNewHost)
                {
                    // Choose a front queue (biased towards priority) 
                    int choice = new Random().NextDouble() > 0.75 ? 1 : 0;

                    Queue<Uri> frontQueue = FrontQueues[choice].Count == 0 ?
                        FrontQueues[choice ^ 1] : // This ONLY works with two front queues. 
                        FrontQueues[choice];

                    Uri frontUri = frontQueue.Dequeue();
                    if (HostToBackQueueMap.ContainsKey(frontUri.Host))
                    {
                        // We already have a queue for this host, so add the link to that.
                        HostToBackQueueMap[frontUri.Host].Enqueue(frontUri);
                    }
                    else
                    {
                        // We can take this host. 
                        HostToBackQueueMap.Add(frontUri.Host, chosenQueue);
                        chosenQueue.Enqueue(frontUri);
                        foundNewHost = true;
                    }
                }
            }

            while (chosenQueue.CanBeAccessedAt > DateTime.Now)
            {
                Thread.Sleep(100);
            }

            return chosenQueue.Dequeue();
        }

        public void RefillBackQueues()
        {
            // Check the next elements of all front queues and examine their host. 
            // If their host exists in the host-to-back-queue table, add them to
            // their respective back queues.

            foreach (var queue in FrontQueues)
            {
                if (queue.Count != 0)
                {
                    Uri nextUri = queue.Peek();
                    if (HostToBackQueueMap.ContainsKey(nextUri.Host))
                    {
                        // Add the URI to its back queue
                        HostToBackQueueMap[nextUri.Host].Enqueue(nextUri);
                    }
                }
            }
        }
    }
}

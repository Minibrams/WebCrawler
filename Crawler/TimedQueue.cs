using System;
using System.Collections.Generic;
using System.Text;

namespace Crawler
{
    class TimedQueue<T> : Queue<T>
    {
        public DateTime CanBeAccessedAt = DateTime.Now;

        public new T Dequeue()
        {
            CanBeAccessedAt = DateTime.Now.AddSeconds(2);
            return base.Dequeue();
        }
    }
}

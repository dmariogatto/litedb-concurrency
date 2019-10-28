using Concurrency.LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Concurrency.Console.LiteDB
{
    class Program
    {
        static void Main(string[] args)
        {
            // Runs very slow, does not corrupt
            // Possibly some internal locking?
            var cache = new CacheLiteDB(AppDomain.CurrentDomain.BaseDirectory, "cache.db");
            var tasks = Enumerable.Range(1, 1000).Select(i => CacheTask.Work(cache, i)).ToList();
            Task.WhenAll(tasks).Wait();
        }
    }
}

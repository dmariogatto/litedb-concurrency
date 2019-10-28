using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Concurrency.LiteDB
{
    public static class CacheTask
    {
        public static async Task Work(ICache cache, int i)
        {
            await Task.Delay(25).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"{i}: EXECUTING");

            var key = $"item_{i}";
            cache.Add(key, Enumerable.Range(1, 100).ToList(), TimeSpan.FromSeconds(1));

            var result = cache.Get<List<int>>(key);
            if (result.Count != 100)
            {
                System.Diagnostics.Debug.WriteLine($"{i}: COUNT DOES NOT MATCH");
            }

            cache.Shrink();

            if (i == 250 || i == 500)
            {
                cache.EmptyExpired();
                cache.Shrink();
            }
        }
    }
}

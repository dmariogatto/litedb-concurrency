using LiteDB;
using System;

namespace Concurrency.LiteDB
{
    public class CacheItem
    {
        [BsonId]
        public string Id { get; set; }
        public string Contents { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}

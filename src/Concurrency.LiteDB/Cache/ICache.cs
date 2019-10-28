using System;
using System.Collections.Generic;
using System.Text;

namespace Concurrency.LiteDB
{
    public enum CacheState
    {
        None = 0,
        Expired = 1,
        Active = 2
    }

    public interface ICache
    {
        bool Add<T>(string key, T data, TimeSpan expireIn);

        bool EmptyAll();
        bool EmptyExpired();

        bool Exists(string key);

        IEnumerable<(string, CacheState)> GetKeys();

        T Get<T>(string key);

        bool IsExpired(string key);
        DateTime? GetExpiration(string key);

        long SizeInBytes();
        bool Shrink();
    }
}

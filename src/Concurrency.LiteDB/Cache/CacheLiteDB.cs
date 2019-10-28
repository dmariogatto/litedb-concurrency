using LiteDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Concurrency.LiteDB
{
    /**
     * Modified from: https://github.com/jamesmontemagno/monkey-cache 
     **/
    public class CacheLiteDB : ICache
    {
        private readonly string _dbPath;
        private readonly string _dbConnString;
        private readonly LiteDatabase _db;
        private readonly LiteCollection<CacheItem> _col;
        private readonly JsonSerializerSettings _jsonSettings;

        public CacheLiteDB(string cacheDirectory, string cacheFileName, string encryptionKey = "")
        {
            if (string.IsNullOrEmpty(cacheDirectory))
                throw new ArgumentException("Directory can not be null or empty.", nameof(cacheDirectory));
            if (string.IsNullOrEmpty(cacheFileName))
                throw new ArgumentException("File name can not be null or empty.", nameof(cacheFileName));
            
            _dbPath = Path.Combine(cacheDirectory, cacheFileName);
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            _dbConnString = !string.IsNullOrWhiteSpace(encryptionKey)
                ? $"Filename={_dbPath}; Password={encryptionKey}"
                : _dbPath;

            _db = new LiteDatabase(_dbConnString);
            _col = _db.GetCollection<CacheItem>();

            _jsonSettings = new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All,
            };
        }

        #region Exist and Expiration Methods        
        public bool Exists(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key can not be null or empty.", nameof(key));

            var item = default(CacheItem);

            try
            {
                item = _col.FindById(key);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return item != default;
        }

        public bool IsExpired(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key can not be null or empty.", nameof(key));

            var item = default(CacheItem);

            try
            {
                item = _col.FindById(key);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return item == null || DateTime.UtcNow > item.ExpirationDate.ToUniversalTime();
        }

        #endregion

        #region Get Methods        
        public IEnumerable<(string, CacheState)> GetKeys()
        {
            var keys = default(List<CacheItem>);

            try
            {
                keys = _col.FindAll().ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return keys != null
                ? keys.Select(i => (i.Id, GetExpiration(i.Id) >= DateTime.UtcNow ? CacheState.Active : CacheState.Expired))
                : Enumerable.Empty<(string, CacheState)>();
        }

        public T Get<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key can not be null or empty.", nameof(key));

            var result = default(T);
            var item = default(CacheItem);

            try
            {
                item = _col.FindById(key);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            if (item == default)
                return result;

            if (IsString(result))
            {
                object final = item.Contents;
                return (T)final;
            }

            return JsonConvert.DeserializeObject<T>(item.Contents, _jsonSettings);
        }

        public DateTime? GetExpiration(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key can not be null or empty.", nameof(key));

            var item = default(CacheItem);

            try
            {
                item = _col.FindById(key);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            if (item == default)
                return null;

            return item.ExpirationDate;
        }

        #endregion

        #region Add Methods
        public bool Add<T>(string key, T data, TimeSpan expireIn)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key can not be null or empty.", nameof(key));

            if (data == null)
                throw new ArgumentNullException("Data can not be null.", nameof(data));

            var dataJson = string.Empty;

            if (IsString(data))
            {
                dataJson = data as string;
            }
            else
            {
                dataJson = JsonConvert.SerializeObject(data, _jsonSettings);
            }

            return Add(key, dataJson, expireIn);
        }
        #endregion

        #region Empty Methods
        public bool EmptyExpired()
        {
            var success = false;

            try
            {
                _col.Delete(b => b.ExpirationDate.ToUniversalTime() < DateTime.UtcNow);
                success = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return success;
        }

        public bool EmptyAll()
        {
            var success = false;

            try
            {
                _col.Delete(Query.All());
                success = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return success;
        }
        #endregion

        public long SizeInBytes()
        {
            var size = 0L;

            if (File.Exists(_dbPath))
            {
                try
                {
                    var fileInfo = new FileInfo(_dbPath);
                    size = fileInfo.Length;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            return size;
        }

        public bool Shrink()
        {
            var success = false;

            try
            {
                _db.Shrink();
                success = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return success;
        }

        private bool Add(string key, string data, TimeSpan expireIn)
        {
            if (data == null)
                return false;

            var success = false;

            try
            {
                var item = new CacheItem
                {
                    Id = key,
                    ExpirationDate = GetExpiration(expireIn),
                    Contents = data
                };

                _col.Upsert(item);

                success = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return success;
        }

        private static bool IsString<T>(T _)
        {
            var typeOf = typeof(T);
            if (typeOf.IsGenericType && typeOf.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                typeOf = Nullable.GetUnderlyingType(typeOf);
            }
            var typeCode = Type.GetTypeCode(typeOf);
            return typeCode == TypeCode.String;
        }

        private static DateTime GetExpiration(TimeSpan timeSpan)
        {
            try
            {
                return DateTime.UtcNow.Add(timeSpan);
            }
            catch
            {
                if (timeSpan.Milliseconds < 0)
                    return DateTime.MinValue;

                return DateTime.MaxValue;
            }
        }
    }
}

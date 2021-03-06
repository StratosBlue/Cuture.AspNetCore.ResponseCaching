﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Cuture.AspNetCore.ResponseCaching.ResponseCaches;

namespace ResponseCaching.Test.RequestTests
{
    public class CountDistributedResponseCache : IDistributedResponseCache
    {
        #region Private 字段

        private readonly ConcurrentDictionary<string, ResponseCacheEntry> _caches = new ConcurrentDictionary<string, ResponseCacheEntry>();

        private readonly Dictionary<string, int> _count = new Dictionary<string, int>();

        #endregion Private 字段

        #region Public 方法

        public int Count(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                lock (_count)
                {
                    return _count.Values.Sum();
                }
            }
            lock (_count)
            {
                if (_count.ContainsKey(key))
                {
                    return _count[key];
                }
            }
            return 0;
        }

        public Task<ResponseCacheEntry> GetAsync(string key)
        {
            if (_caches.TryGetValue(key, out var cacheEntry))
            {
                if (!cacheEntry.IsExpired())
                {
                    lock (_count)
                    {
                        if (_count.ContainsKey(key))
                        {
                            _count[key] += 1;
                        }
                        else
                        {
                            _count[key] = 1;
                        }
                    }
                    return Task.FromResult(cacheEntry);
                }
            }
            return Task.FromResult((ResponseCacheEntry)null);
        }

        public string[] GetKeys() => _count.Keys.ToArray();

        public Task SetAsync(string key, ResponseCacheEntry entry)
        {
            _caches.AddOrUpdate(key, entry, (_, _) => entry);
            return Task.CompletedTask;
        }

        #endregion Public 方法
    }
}
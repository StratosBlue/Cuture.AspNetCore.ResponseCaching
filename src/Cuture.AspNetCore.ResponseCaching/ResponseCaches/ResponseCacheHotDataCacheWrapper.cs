﻿using System;
using System.Threading.Tasks;

namespace Cuture.AspNetCore.ResponseCaching.ResponseCaches
{
    /// <summary>
    /// 响应缓存的热数据缓存包装器
    /// </summary>
    public sealed class ResponseCacheHotDataCacheWrapper : IDistributedResponseCache, IDisposable
    {
        #region Private 字段

        private readonly IDistributedResponseCache _distributedCache;
        private readonly IHotDataCache _hotDataCache;

        #endregion Private 字段

        #region Public 构造函数

        /// <inheritdoc cref="ResponseCacheHotDataCacheWrapper"/>
        public ResponseCacheHotDataCacheWrapper(IDistributedResponseCache distributedCache, IHotDataCache hotDataCache)
        {
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _hotDataCache = hotDataCache ?? throw new ArgumentNullException(nameof(hotDataCache));
        }

        #endregion Public 构造函数

        #region Public 方法

        /// <inheritdoc/>
        public void Dispose()
        {
            _hotDataCache.Dispose();
        }

        /// <inheritdoc/>
        public async Task<ResponseCacheEntry?> GetAsync(string key)
        {
            //HACK 此处未加锁，并发访问会穿透本地缓存
            var cacheEntry = _hotDataCache.Get(key);
            if (cacheEntry is not null
                && !cacheEntry.IsExpired())
            {
                return cacheEntry;
            }
            cacheEntry = await _distributedCache.GetAsync(key);
            if (cacheEntry is not null)
            {
                _hotDataCache.Set(key, cacheEntry);
            }
            return cacheEntry;
        }

        /// <inheritdoc/>
        public Task SetAsync(string key, ResponseCacheEntry entry)
        {
            _hotDataCache.Set(key, entry);
            return _distributedCache.SetAsync(key, entry);
        }

        #endregion Public 方法
    }
}
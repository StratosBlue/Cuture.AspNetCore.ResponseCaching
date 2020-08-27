﻿using System;

using Cuture.AspNetCore.ResponseCaching.CacheKey.Generators;
using Cuture.AspNetCore.ResponseCaching.Lockers;
using Cuture.AspNetCore.ResponseCaching.ResponseCaches;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Cuture.AspNetCore.ResponseCaching
{
    /// <summary>
    /// 响应缓存上下文
    /// </summary>
    /// <typeparam name="TFilterContext">FilterContext</typeparam>
    /// <typeparam name="TLocalCachingData">本地缓存类型</typeparam>
    public class ResponseCachingContext<TFilterContext, TLocalCachingData> where TFilterContext : FilterContext
    {
        private readonly ResponseCachingAttribute _cachingAttribute;

        /// <summary>
        /// 缓存Key生成器
        /// </summary>
        public ICacheKeyGenerator KeyGenerator { get; }

        /// <summary>
        /// 响应缓存容器
        /// </summary>
        public IResponseCache ResponseCache { get; }

        /// <summary>
        /// 执行锁定器
        /// </summary>
        public IRequestExecutingLocker<TFilterContext, TLocalCachingData> ExecutingLocker { get; }

        /// <summary>
        /// 响应缓存确定器
        /// </summary>
        public IResponseCacheDeterminer CacheDeterminer { get; set; }

        /// <summary>
        /// 缓存有效时长（秒）
        /// </summary>
        public int Duration { get; }

        /// <summary>
        /// 缓存Key的最大长度
        /// </summary>
        public int MaxCacheKeyLength { get; }

        /// <summary>
        /// 最大可缓存响应长度
        /// </summary>
        public int MaxCacheableResponseLength { get; } = -1;

        /// <summary>
        /// 响应转储Stream工厂
        /// </summary>
        public IDumpStreamFactory DumpStreamFactory { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="cachingAttribute"></param>
        /// <param name="cacheKeyGenerator"></param>
        /// <param name="executingLocker"></param>
        /// <param name="responseCache"></param>
        /// <param name="cacheDeterminer"></param>
        /// <param name="optionsAccessor"></param>
        public ResponseCachingContext(ResponseCachingAttribute cachingAttribute,
                                      ICacheKeyGenerator cacheKeyGenerator,
                                      IRequestExecutingLocker<TFilterContext, TLocalCachingData> executingLocker,
                                      IResponseCache responseCache,
                                      IResponseCacheDeterminer cacheDeterminer,
                                      IOptions<ResponseCachingOptions> optionsAccessor)
        {
            var options = optionsAccessor.Value;

            _cachingAttribute = cachingAttribute ?? throw new ArgumentNullException(nameof(cachingAttribute));

            MaxCacheableResponseLength = cachingAttribute.MaxCacheableResponseLength;
            MaxCacheableResponseLength = MaxCacheableResponseLength >= ResponseCachingConstants.DefaultMinMaxCacheableResponseLength
                                            ? MaxCacheableResponseLength
                                            : MaxCacheableResponseLength == -1
                                                ? options.MaxCacheableResponseLength
                                                : throw new ArgumentOutOfRangeException($"Unavailable value of {nameof(MaxCacheableResponseLength)}");
            MaxCacheKeyLength = options.MaxCacheKeyLength;

            KeyGenerator = cacheKeyGenerator ?? throw new ArgumentNullException(nameof(cacheKeyGenerator));
            ExecutingLocker = executingLocker;
            ResponseCache = responseCache ?? throw new ArgumentNullException(nameof(responseCache));
            CacheDeterminer = cacheDeterminer ?? throw new ArgumentNullException(nameof(cacheDeterminer));
            Duration = cachingAttribute.Duration > 1 ? cachingAttribute.Duration : throw new ArgumentOutOfRangeException($"{nameof(cachingAttribute.Duration)}  can not less than {ResponseCachingConstants.MinCacheAvailableSeconds} seconds");

            DumpStreamFactory = new DefaultDumpStreamFactory(ResponseCachingConstants.DefaultMinMaxCacheableResponseLength * 2);
        }
    }
}
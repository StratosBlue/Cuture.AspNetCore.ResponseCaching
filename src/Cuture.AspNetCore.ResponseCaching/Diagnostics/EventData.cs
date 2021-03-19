﻿// <Auto-Generated></Auto-Generated>

using System;

using Cuture.AspNetCore.ResponseCaching.CacheKey.Generators;
using Cuture.AspNetCore.ResponseCaching.ResponseCaches;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cuture.AspNetCore.ResponseCaching.Diagnostics
{
    /// <summary>
    /// 诊断事件数据
    /// </summary>
    public abstract class ResponseCachingEventData
    {
        /// <summary>
        /// 诊断名称
        /// </summary>
        public const string DiagnosticName = "Cuture.AspNetCore.ResponseCaching";

        /// <summary>
        /// 响应缓存的处理上下文
        /// <see cref="ResponseCachingContext{TFilterContext, TLocalCachingData}"/>
        /// <para/>
        /// 默认情况下TFilterContext可能为<see cref="ActionExecutingContext"/>或<see cref="ResourceExecutingContext"/>
        /// <para/>
        /// 与之对应的TLocalCachingData分别为<see cref="IActionResult"/>和<see cref="ResponseCacheEntry"/>
        /// </summary>
        public object Context { get; }

        /// <summary>
        /// <inheritdoc cref="ResponseCachingEventData"/>
        /// </summary>
        /// <param name="context">响应缓存的处理上下文</param>
        public ResponseCachingEventData(object context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }
    }

    /// <summary>
    /// 开始处理缓存事件
    /// </summary>
    public class StartProcessingCacheEventData : ResponseCachingEventData
    {
        /// <summary>
        /// <inheritdoc cref="StartProcessingCacheEventData"/>名称
        /// </summary>
        public const string EventName = DiagnosticName + ".StartProcessingCache";

        /// <inheritdoc cref="Microsoft.AspNetCore.Mvc.Filters.FilterContext"/>
        public FilterContext FilterContext { get; }

        /// <inheritdoc cref="StartProcessingCacheEventData"/>
        public StartProcessingCacheEventData(FilterContext filterContext, object context) : base(context)
        {
            FilterContext = filterContext;
        }
    }

    /// <summary>
    /// 处理缓存结束事件
    /// </summary>
    public class EndProcessingCacheEventData : ResponseCachingEventData
    {
        /// <summary>
        /// <inheritdoc cref="EndProcessingCacheEventData"/>名称
        /// </summary>
        public const string EventName = DiagnosticName + ".EndProcessingCache";

        /// <inheritdoc cref="Microsoft.AspNetCore.Mvc.Filters.FilterContext"/>
        public FilterContext FilterContext { get; }

        /// <inheritdoc cref="EndProcessingCacheEventData"/>
        public EndProcessingCacheEventData(FilterContext filterContext, object context) : base(context)
        {
            FilterContext = filterContext;
        }
    }

    /// <summary>
    /// 缓存key已生成事件
    /// </summary>
    public class CacheKeyGeneratedEventData : ResponseCachingEventData
    {
        /// <summary>
        /// <inheritdoc cref="CacheKeyGeneratedEventData"/>名称
        /// </summary>
        public const string EventName = DiagnosticName + ".CacheKeyGenerated";

        /// <summary>
        /// <see cref="ResourceExecutingContext"/>或<see cref="ActionExecutingContext"/>
        /// </summary>
        public FilterContext FilterContext { get; }

        /// <summary>
        /// 缓存键
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// 使用的缓存键生成器
        /// </summary>
        public ICacheKeyGenerator KeyGenerator { get; }

        /// <inheritdoc cref="CacheKeyGeneratedEventData"/>
        public CacheKeyGeneratedEventData(FilterContext filterContext, string key, ICacheKeyGenerator keyGenerator, object context) : base(context)
        {
            FilterContext = filterContext;
            Key = key;
            KeyGenerator = keyGenerator;
        }
    }

    /// <summary>
    /// 从缓存响应请求事件
    /// </summary>
    public class ResponseFromCacheEventData : ResponseCachingEventData
    {
        /// <summary>
        /// <inheritdoc cref="ResponseFromCacheEventData"/>名称
        /// </summary>
        public const string EventName = DiagnosticName + ".ResponseFromCache";

        /// <inheritdoc cref="Microsoft.AspNetCore.Mvc.ActionContext"/>
        public ActionContext ActionContext { get; }

        /// <summary>
        /// 缓存项
        /// </summary>
        public ResponseCacheEntry CacheEntry { get; }

        /// <inheritdoc cref="ResponseFromCacheEventData"/>
        public ResponseFromCacheEventData(ActionContext actionContext, ResponseCacheEntry cacheEntry, object context) : base(context)
        {
            ActionContext = actionContext;
            CacheEntry = cacheEntry;
        }
    }

    /// <summary>
    /// 使用<see cref="IActionResult"/>响应请求事件
    /// </summary>
    public class ResponseFromActionResultEventData : ResponseCachingEventData
    {
        /// <summary>
        /// <inheritdoc cref="ResponseFromActionResultEventData"/>名称
        /// </summary>
        public const string EventName = DiagnosticName + ".ResponseFromActionResult";

        /// <inheritdoc cref="ActionExecutingContext"/>
        public ActionExecutingContext ActionExecutingContext { get; }

        /// <inheritdoc cref="IActionResult"/>
        public IActionResult ActionResult { get; }

        /// <inheritdoc cref="ResponseFromActionResultEventData"/>
        public ResponseFromActionResultEventData(ActionExecutingContext executingContext, IActionResult actionResult, object context) : base(context)
        {
            ActionExecutingContext = executingContext;
            ActionResult = actionResult;
        }
    }

    /// <summary>
    /// 缓存键过长事件
    /// </summary>
    public class CacheKeyTooLongEventData : ResponseCachingEventData
    {
        /// <summary>
        /// <inheritdoc cref="CacheKeyTooLongEventData"/>名称
        /// </summary>
        public const string EventName = DiagnosticName + ".CacheKeyTooLong";

        /// <summary>
        /// key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 最长可用长度
        /// </summary>
        public int MaxAvailableLength { get; }

        /// <inheritdoc cref="Microsoft.AspNetCore.Mvc.Filters.FilterContext"/>
        public FilterContext FilterContext { get; }

        /// <inheritdoc cref="CacheKeyTooLongEventData"/>
        public CacheKeyTooLongEventData(string key, int maxAvailableLength, FilterContext filterContext, object context) : base(context)
        {
            Key = key;
            MaxAvailableLength = maxAvailableLength;
            FilterContext = filterContext;
        }
    }

    /// <summary>
    /// 没有找到缓存事件（需要执行正常流程）
    /// </summary>
    public class NoCachingFoundedEventData : ResponseCachingEventData
    {
        /// <summary>
        /// <inheritdoc cref="NoCachingFoundedEventData"/>名称
        /// </summary>
        public const string EventName = DiagnosticName + ".NoCachingFounded";

        /// <summary>
        /// key
        /// </summary>
        public string Key { get; set; }

        /// <inheritdoc cref="Microsoft.AspNetCore.Mvc.Filters.FilterContext"/>
        public FilterContext FilterContext { get; }

        /// <inheritdoc cref="NoCachingFoundedEventData"/>
        public NoCachingFoundedEventData(string key, FilterContext filterContext, object context) : base(context)
        {
            Key = key;
            FilterContext = filterContext;
        }
    }

    /// <summary>
    /// 缓存内容过大事件
    /// </summary>
    public class CacheBodyTooLargeEventData : ResponseCachingEventData
    {
        /// <summary>
        /// <inheritdoc cref="CacheBodyTooLargeEventData"/>名称
        /// </summary>
        public const string EventName = DiagnosticName + ".CacheBodyTooLarge";

        /// <summary>
        /// key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        public ReadOnlyMemory<byte> Body { get; }

        /// <summary>
        /// 最长可用长度
        /// </summary>
        public int MaxAvailableLength { get; }

        /// <inheritdoc cref="Microsoft.AspNetCore.Mvc.Filters.FilterContext"/>
        public FilterContext FilterContext { get; }

        /// <inheritdoc cref="CacheBodyTooLargeEventData"/>
        public CacheBodyTooLargeEventData(string key, ReadOnlyMemory<byte> body, int maxAvailableLength, FilterContext filterContext, object context) : base(context)
        {
            Key = key;
            Body = body;
            MaxAvailableLength = maxAvailableLength;
            FilterContext = filterContext;
        }
    }

    /// <summary>
    /// 无法使用锁执行请求
    /// </summary>
    public class CannotExecutionThroughLockEventData : ResponseCachingEventData
    {
        /// <summary>
        /// <inheritdoc cref="CannotExecutionThroughLockEventData"/>名称
        /// </summary>
        public const string EventName = DiagnosticName + ".CannotExecutionThroughLock";

        /// <summary>
        /// key
        /// </summary>
        public string Key { get; set; }

        /// <inheritdoc cref="Microsoft.AspNetCore.Mvc.Filters.FilterContext"/>
        public FilterContext FilterContext { get; }

        /// <inheritdoc cref="CannotExecutionThroughLockEventData"/>
        public CannotExecutionThroughLockEventData(string key, FilterContext filterContext, object context) : base(context)
        {
            Key = key;
            FilterContext = filterContext;
        }
    }
}
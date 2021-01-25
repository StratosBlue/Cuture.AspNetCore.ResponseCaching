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
        /// 事件名称
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
        /// 事件名称
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
        /// 事件名称
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
    /// 使用缓存响应事件
    /// </summary>
    public class ResponseFromCacheEventData : ResponseCachingEventData
    {
        /// <summary>
        /// 事件名称
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
    /// 使用<see cref="ActionResult"/>响应事件
    /// </summary>
    public class ResponseFromActionResultEventData : ResponseCachingEventData
    {
        /// <summary>
        /// 事件名称
        /// </summary>
        public const string EventName = DiagnosticName + ".ResponseFromActionResult";

        /// <inheritdoc cref="ActionExecutingContext"/>
        public ActionExecutingContext ActionExecutingContext { get; }

        /// <summary>
        /// ActionResult
        /// </summary>
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
        /// 事件名称
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

        /// <inheritdoc cref="CacheKeyTooLongEventData"/>
        public CacheKeyTooLongEventData(string key, int maxAvailableLength, object context) : base(context)
        {
            Key = key;
            MaxAvailableLength = maxAvailableLength;
        }
    }

    /// <summary>
    /// 缓存内容过长事件
    /// </summary>
    public class CacheBodyTooLongEventData : ResponseCachingEventData
    {
        /// <summary>
        /// 事件名称
        /// </summary>
        public const string EventName = DiagnosticName + ".CacheBodyTooLong";

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

        /// <inheritdoc cref="ActionContext"/>
        public ActionContext ActionContext { get; }

        /// <inheritdoc cref="CacheBodyTooLongEventData"/>
        public CacheBodyTooLongEventData(string key, ReadOnlyMemory<byte> body, int maxAvailableLength, ActionContext actionContext, object context) : base(context)
        {
            Key = key;
            Body = body;
            MaxAvailableLength = maxAvailableLength;
            ActionContext = actionContext;
        }
    }
}
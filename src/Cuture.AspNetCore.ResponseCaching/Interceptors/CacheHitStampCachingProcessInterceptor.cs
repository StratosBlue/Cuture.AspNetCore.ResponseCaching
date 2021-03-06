﻿using System;
using System.Threading.Tasks;

using Cuture.AspNetCore.ResponseCaching.ResponseCaches;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace Cuture.AspNetCore.ResponseCaching.Interceptors
{
    /// <summary>
    /// 缓存处理拦截器 - 缓存命中标记响应头
    /// </summary>
    internal class CacheHitStampCachingProcessInterceptor : CachingProcessInterceptor
    {
        #region Private 字段

        private readonly string _headerKey;
        private readonly StringValues _headerValue;

        #endregion Private 字段

        #region Public 构造函数

        /// <summary>
        /// 缓存处理拦截器 - 缓存命中标记响应头
        /// </summary>
        /// <param name="headerKey"></param>
        /// <param name="headerValue"></param>
        public CacheHitStampCachingProcessInterceptor(string headerKey, StringValues headerValue)
        {
            if (string.IsNullOrEmpty(headerKey))
            {
                throw new ArgumentException($"“{nameof(headerKey)}”不能是 Null 或为空", nameof(headerKey));
            }

            if (string.IsNullOrEmpty(headerValue))
            {
                throw new ArgumentException($"“{nameof(headerValue)}”不能是 Null 或为空", nameof(headerValue));
            }

            _headerKey = headerKey;
            _headerValue = headerValue;
        }

        #endregion Public 构造函数

        #region Public 方法

        /// <inheritdoc/>
        public override Task<bool> OnResponseWritingAsync(ActionContext actionContext, ResponseCacheEntry entry, Func<ActionContext, ResponseCacheEntry, Task<bool>> writeFunc)
        {
            actionContext.HttpContext.Response.Headers.Add(_headerKey, _headerValue);
            return base.OnResponseWritingAsync(actionContext, entry, writeFunc);
        }

        /// <inheritdoc/>
        public override Task OnResultSettingAsync(ActionExecutingContext actionContext, IActionResult actionResult, Func<ActionExecutingContext, IActionResult, Task> setResultFunc)
        {
            actionContext.HttpContext.Response.Headers.Add(_headerKey, _headerValue);
            return base.OnResultSettingAsync(actionContext, actionResult, setResultFunc);
        }

        #endregion Public 方法
    }
}
﻿namespace Cuture.AspNetCore.ResponseCaching.CacheKey.Generators
{
    /// <summary>
    /// 自定义缓存Key生成器
    /// </summary>
    public interface ICustomCacheKeyGenerator : ICacheKeyGenerator
    {
        #region Public 属性

        /// <summary>
        /// 过滤器类型
        /// </summary>
        FilterType FilterType { get; }

        #endregion Public 属性
    }
}
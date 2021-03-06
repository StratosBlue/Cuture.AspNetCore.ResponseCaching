﻿using System;

using Cuture.AspNetCore.ResponseCaching.CacheKey.Builders;
using Cuture.AspNetCore.ResponseCaching.CacheKey.Generators;
using Cuture.AspNetCore.ResponseCaching.Diagnostics;
using Cuture.AspNetCore.ResponseCaching.Filters;
using Cuture.AspNetCore.ResponseCaching.Interceptors;
using Cuture.AspNetCore.ResponseCaching.Lockers;
using Cuture.AspNetCore.ResponseCaching.ResponseCaches;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cuture.AspNetCore.ResponseCaching
{
    /// <inheritdoc/>
    internal class DefaultResponseCachingFilterBuilder : IResponseCachingFilterBuilder
    {
        #region Public 方法

        /// <inheritdoc/>
        public IFilterMetadata CreateFilter(IServiceProvider serviceProvider, object context)
        {
            if (context is not ResponseCachingAttribute attribute)
            {
                throw new ArgumentException($"{nameof(DefaultResponseCachingFilterBuilder)} only can build filter from {nameof(ResponseCachingAttribute)}", nameof(context));
            }

            var buildContext = new FilterBuildContext(serviceProvider, attribute);

            if (!buildContext.Options.Enable)
            {
                return EmptyFilterMetadata.Instance;
            }

            CheckDuration(attribute.Duration);

            IResponseCache responseCache = GetResponseCache(buildContext);

            ICacheKeyGenerator cacheKeyGenerator = TryGetCustomCacheKeyGenerator(buildContext, out FilterType filterType)
                                                   ?? CreateCacheKeyGenerator(buildContext, out filterType);

            var cacheDeterminer = serviceProvider.GetRequiredService<IResponseCacheDeterminer>();

            var cachingDiagnosticsAccessor = serviceProvider.GetRequiredService<CachingDiagnosticsAccessor>();

            var interceptorAggregator = new InterceptorAggregator(GetCachingProcessInterceptor(buildContext));

            var executingLockAttribute = buildContext.GetHttpContextMetadata<ExecutingLockAttribute>();

            var lockMode = executingLockAttribute?.LockMode ?? buildContext.Options.DefaultExecutingLockMode;

            var executingLockerName = executingLockAttribute?.LockerName ?? string.Empty;

            switch (filterType)
            {
                case FilterType.Resource:
                    {
                        Type? executingLockerType = lockMode switch
                        {
                            ExecutingLockMode.ActionSingle => typeof(IActionSingleResourceExecutingLocker),
                            ExecutingLockMode.CacheKeySingle => typeof(ICacheKeySingleResourceExecutingLocker),
                            _ => null,
                        };
                        var executingLocker = executingLockerType is null
                                                ? null
                                                : serviceProvider.GetRequiredService<IExecutingLockerProvider>().GetLocker<IRequestExecutingLocker<ResourceExecutingContext, ResponseCacheEntry>>(executingLockerType, executingLockerName);
                        var responseCachingContext = new ResponseCachingContext<ResourceExecutingContext, ResponseCacheEntry>(attribute,
                                                                                                                              cacheKeyGenerator,
                                                                                                                              executingLocker!,
                                                                                                                              responseCache,
                                                                                                                              cacheDeterminer,
                                                                                                                              buildContext.Options,
                                                                                                                              interceptorAggregator);
                        return new DefaultResourceCacheFilter(responseCachingContext, cachingDiagnosticsAccessor);
                    }
                case FilterType.Action:
                    {
                        Type? executingLockerType = lockMode switch
                        {
                            ExecutingLockMode.ActionSingle => typeof(IActionSingleActionExecutingLocker),
                            ExecutingLockMode.CacheKeySingle => typeof(ICacheKeySingleActionExecutingLocker),
                            _ => null,
                        };
                        var executingLocker = executingLockerType is null
                                                ? null
                                                : serviceProvider.GetRequiredService<IExecutingLockerProvider>().GetLocker<IRequestExecutingLocker<ActionExecutingContext, IActionResult>>(executingLockerType, executingLockerName);
                        var responseCachingContext = new ResponseCachingContext<ActionExecutingContext, IActionResult>(attribute,
                                                                                                                       cacheKeyGenerator,
                                                                                                                       executingLocker!,
                                                                                                                       responseCache,
                                                                                                                       cacheDeterminer,
                                                                                                                       buildContext.Options,
                                                                                                                       interceptorAggregator);
                        return new DefaultActionCacheFilter(responseCachingContext, cachingDiagnosticsAccessor);
                    }
                default:
                    throw new NotImplementedException($"Not ready to support FilterType: {filterType}");
            }
        }

        #endregion Public 方法

        #region Private 方法

        private static void CheckDuration(int duration)
        {
            if (duration < ResponseCachingConstants.MinCacheAvailableSeconds)
            {
                throw new ArgumentOutOfRangeException($"{nameof(duration)} can not less than {ResponseCachingConstants.MinCacheAvailableSeconds} second");
            }
        }

        /// <summary>
        /// 创建CacheKeyGenerator
        /// </summary>
        /// <param name="context"></param>
        /// <param name="filterType"></param>
        /// <returns></returns>
        private static ICacheKeyGenerator CreateCacheKeyGenerator(FilterBuildContext context, out FilterType filterType)
        {
            var attribute = context.Attribute;

            var strictMode = attribute.StrictMode == CacheKeyStrictMode.Default
                                ? context.Options.DefaultStrictMode
                                : attribute.StrictMode;

            filterType = FilterType.Resource;

            ICacheKeyGenerator cacheKeyGenerator;
            switch (attribute.Mode)
            {
                case CacheMode.FullPathAndQuery:
                    {
                        cacheKeyGenerator = context.GetRequiredService<FullPathAndQueryCacheKeyGenerator>();
                        break;
                    }

                case CacheMode.Custom:
                    {
                        CacheKeyBuilder? keyBuilder = null;
                        if (attribute.VaryByHeaders?.Length > 0)
                        {
                            keyBuilder = new RequestHeadersCacheKeyBuilder(keyBuilder, strictMode, attribute.VaryByHeaders);
                        }
                        if (attribute.VaryByClaims?.Length > 0)
                        {
                            keyBuilder = new ClaimsCacheKeyBuilder(keyBuilder, strictMode, attribute.VaryByClaims);
                        }
                        if (attribute.VaryByQueryKeys?.Length > 0)
                        {
                            keyBuilder = new QueryKeysCacheKeyBuilder(keyBuilder, strictMode, attribute.VaryByQueryKeys);
                        }
                        if (attribute.VaryByFormKeys?.Length > 0)
                        {
                            keyBuilder = new FormKeysCacheKeyBuilder(keyBuilder, strictMode, attribute.VaryByFormKeys);
                        }
                        if (attribute.VaryByModels != null)
                        {
                            var modelKeyParserType = attribute.ModelKeyParserType ?? typeof(DefaultModelKeyParser);
                            var modelKeyParser = context.GetRequiredService<IModelKeyParser>(modelKeyParserType);
                            keyBuilder = new ModelCacheKeyBuilder(keyBuilder, strictMode, attribute.VaryByModels, modelKeyParser);
                            filterType = FilterType.Action;
                        }

                        if (keyBuilder is null)
                        {
                            throw new ArgumentException("Custom CacheMode must has keys than 1");
                        }

                        cacheKeyGenerator = new DefaultCacheKeyGenerator(keyBuilder);
                        break;
                    }

                case CacheMode.PathUniqueness:
                    {
                        cacheKeyGenerator = context.GetRequiredService<RequestPathCacheKeyGenerator>();
                        break;
                    }

                default:
                    throw new NotImplementedException($"Not ready to support CacheMode: {attribute.Mode}");
            }

            return cacheKeyGenerator;
        }

        /// <summary>
        /// 获取<see cref="ICachingProcessInterceptor"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static ICachingProcessInterceptor? GetCachingProcessInterceptor(FilterBuildContext context)
        {
            var type = context.Attribute.CachingProcessInterceptorType
                            ?? context.GetRequiredService<IOptions<InterceptorOptions>>().Value.CachingProcessInterceptorType;

            return type is null
                        ? null
                        : context.GetRequiredService<ICachingProcessInterceptor>(type);
        }

        /// <summary>
        /// 获取响应缓存容器
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static IResponseCache GetResponseCache(FilterBuildContext context)
        {
            var storeLocation = context.Attribute.StoreLocation == CacheStoreLocation.Default
                                    ? context.Options.DefaultCacheStoreLocation
                                    : context.Attribute.StoreLocation;

            switch (storeLocation)
            {
                case CacheStoreLocation.Distributed:
                    {
                        var responseCache = context.GetRequiredService<IDistributedResponseCache>();
                        var hotDataCacheBuilder = context.GetHttpContextMetadata<IHotDataCacheBuilder>();
                        if (hotDataCacheBuilder is not null)
                        {
                            var hotDataCache = hotDataCacheBuilder.Build(context.ServiceProvider);
                            if (hotDataCache is null)
                            {
                                throw new ResponseCachingException($"The data cache {hotDataCacheBuilder.GetType()} provided is null.");
                            }
                            return new ResponseCacheHotDataCacheWrapper(responseCache, hotDataCache);
                        }

                        return responseCache;
                    }
                case CacheStoreLocation.Memory:
                    return context.GetRequiredService<IMemoryResponseCache>();

                case CacheStoreLocation.Default:
                default:
                    throw new ArgumentException($"UnSupport cache location {storeLocation}");
            }
        }

        /// <summary>
        /// 尝试获取自定义缓存键生成器
        /// </summary>
        /// <param name="context"></param>
        /// <param name="filterType"></param>
        /// <returns></returns>
        private static ICacheKeyGenerator? TryGetCustomCacheKeyGenerator(FilterBuildContext context, out FilterType filterType)
        {
            filterType = FilterType.Resource;

            var attribute = context.Attribute;
            if (attribute.CustomCacheKeyGeneratorType == null)
            {
                return null;
            }

            if (!typeof(ICustomCacheKeyGenerator).IsAssignableFrom(attribute.CustomCacheKeyGeneratorType))
            {
                throw new ArgumentException($"type of {attribute.CustomCacheKeyGeneratorType} must derives from {nameof(ICustomCacheKeyGenerator)}");
            }

            ICustomCacheKeyGenerator cacheKeyGenerator = context.GetRequiredService<ICustomCacheKeyGenerator>(attribute.CustomCacheKeyGeneratorType);
            filterType = cacheKeyGenerator.FilterType;

            if (cacheKeyGenerator is IResponseCachingAttributeSetter responseCachingAttributeSetter)
            {
                responseCachingAttributeSetter.SetResponseCachingAttribute(attribute);
            }

            return cacheKeyGenerator;
        }

        #endregion Private 方法
    }

    internal class FilterBuildContext
    {
        #region Private 字段

        private Endpoint? _endpoint;

        #endregion Private 字段

        #region Public 属性

        public ResponseCachingAttribute Attribute { get; }

        public Endpoint Endpoint => _endpoint ?? GetEndpoint();

        public ResponseCachingOptions Options { get; }

        public IServiceProvider ServiceProvider { get; }

        #endregion Public 属性

        #region Public 构造函数

        public FilterBuildContext(IServiceProvider serviceProvider, ResponseCachingAttribute attribute)
        {
            ServiceProvider = serviceProvider;
            Attribute = attribute;

            Options = serviceProvider.GetRequiredService<IOptions<ResponseCachingOptions>>().Value;
        }

        #endregion Public 构造函数

        #region Public 方法

        public T? GetHttpContextMetadata<T>() where T : class => Endpoint.Metadata.GetMetadata<T>();

        public T GetRequiredService<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();

        public T GetRequiredService<T>(Type type) => (T)ServiceProvider.GetRequiredService(type);

        #endregion Public 方法

        #region Private 方法

        private Endpoint GetEndpoint()
        {
            var endpoint = ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext?.GetEndpoint();
            if (endpoint is null)
            {
                throw new ResponseCachingException("Cannot access Endpoint by IHttpContextAccessor.");
            }
            _endpoint = endpoint;
            return endpoint;
        }

        #endregion Private 方法
    }
}
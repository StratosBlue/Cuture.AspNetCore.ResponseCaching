﻿using System;
using System.Threading;

using Cuture.AspNetCore.ResponseCaching;
using Cuture.AspNetCore.ResponseCaching.CacheKey.Builders;
using Cuture.AspNetCore.ResponseCaching.CacheKey.Generators;
using Cuture.AspNetCore.ResponseCaching.Filters;
using Cuture.AspNetCore.ResponseCaching.Lockers;
using Cuture.AspNetCore.ResponseCaching.ResponseCaches;

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// 响应缓存
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ResponseCachingAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        #region Private 字段

        private SpinLock _createInstanceLock = new SpinLock(false);

        private IFilterMetadata _filterMetadata;

        #endregion Private 字段

        #region Public 属性

        /// <summary>
        /// 自定义缓存键生成器类型
        /// <para/>
        /// 需要实现 <see cref="ICustomCacheKeyGenerator"/> 接口
        /// <para/>
        /// 需要Attribute数据时实现 <see cref="IResponseCachingAttributeSetter"/> 接口
        /// </summary>
        public Type CustomCacheKeyGeneratorType { get; set; }

        /// <summary>
        /// 缓存时长（秒）
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        ///
        /// </summary>
        public bool IsReusable => true;

        /// <summary>
        /// 缓存通行模式（设置执行action的并发控制）
        /// <para/>
        /// Note:
        /// <para/>
        /// * 越细粒度的控制会带来相对更多的性能消耗
        /// <para/>
        /// * 虽然已经尽可能的实现了并发控制，仍然最好不要依赖此功能实现具体业务
        /// </summary>
        public ExecutingLockMode LockMode { get; set; } = ExecutingLockMode.Default;

        /// <summary>
        /// 最大可缓存响应长度（默认使用全局配置）
        /// </summary>
        public int MaxCacheableResponseLength { get; set; } = -1;

        /// <summary>
        /// 缓存模式（设置依据什么内容进行缓存）
        /// </summary>
        public CacheMode Mode { get; set; } = CacheMode.Default;

        /// <summary>
        /// Model的Key解析器类型
        /// <para/>
        /// 需要实现 <see cref="IModelKeyParser"/> 接口
        /// </summary>
        public Type ModelKeyParserType { get; set; }

        /// <summary>
        /// Filter排序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 缓存数据存储位置
        /// </summary>
        public CacheStoreLocation StoreLocation { get; set; } = CacheStoreLocation.Default;

        /// <summary>
        /// 缓存键严格模式（指定键找不到时的处理方式）
        /// </summary>
        public CacheKeyStrictMode StrictMode { get; set; } = CacheKeyStrictMode.Default;

        /// <summary>
        /// 依据声明
        /// </summary>
        public string[] VaryByClaims { get; set; }

        /// <summary>
        /// 依据表单键
        /// </summary>
        public string[] VaryByFormKeys { get; set; }

        /// <summary>
        /// 依据请求头
        /// </summary>
        public string[] VaryByHeaders { get; set; }

        /// <summary>
        /// 依据Model
        /// <para/>
        /// Note:
        /// <para/>
        /// * 以下为使用默认实现时的情况
        /// <para/>
        /// * 使用空数组时为获取所有model进行生成Key
        /// <para/>
        /// * 使用的Filter将会从 <see cref="IAsyncResourceFilter"/> 转变为 <see cref="IAsyncActionFilter"/> &amp; <see cref="IAsyncResourceFilter"/>
        /// <para/>
        /// * 由于内部的实现问题，<see cref="LockMode"/> 的设置在某些情况下可能无法严格限制所有请求
        /// <para/>
        /// * 生成Key时，如果没有指定 <see cref="ModelKeyParserType"/>，
        /// 则检查Model是否实现 <see cref="ICacheKeyable"/> 接口，如果Model未实现 <see cref="ICacheKeyable"/> 接口，
        /// 则调用Model的 <see cref="object.ToString"/> 方法生成Key
        /// </summary>
        public string[] VaryByModels { get; set; }

        /// <summary>
        /// 依据查询键
        /// </summary>
        public string[] VaryByQueryKeys { get; set; }

        #endregion Public 属性

        #region Public 构造函数

        /// <summary>
        /// 响应缓存
        /// </summary>
        public ResponseCachingAttribute()
        {
        }

        /// <summary>
        /// 响应缓存
        /// </summary>
        /// <param name="duration">缓存时长（秒）</param>
        public ResponseCachingAttribute(int duration)
        {
            Duration = duration;
        }

        #endregion Public 构造函数

        #region Public 方法

        /// <summary>
        /// 创建 <see cref="IFilterMetadata"/>
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var locked = false;
            try
            {
                _createInstanceLock.Enter(ref locked);
                if (_filterMetadata is null)
                {
                    _filterMetadata = CreateFilter(serviceProvider);
                }
                return _filterMetadata;
            }
            finally
            {
                if (locked)
                {
                    _createInstanceLock.Exit();
                }
            }
        }

        #endregion Public 方法

        #region CreateFilter

        /// <summary>
        /// 创建CacheKeyGenerator
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="options"></param>
        /// <param name="filterType"></param>
        /// <returns></returns>
        private ICacheKeyGenerator CreateCacheKeyGenerator(IServiceProvider serviceProvider, ResponseCachingOptions options, out FilterType filterType)
        {
            var strictMode = StrictMode == CacheKeyStrictMode.Default ? options.DefaultStrictMode : StrictMode;
            filterType = FilterType.Resource;

            ICacheKeyGenerator cacheKeyGenerator;
            switch (Mode)
            {
                case CacheMode.FullPathAndQuery:
                    {
                        cacheKeyGenerator = serviceProvider.GetRequiredService<FullPathAndQueryCacheKeyGenerator>();
                        break;
                    }

                case CacheMode.Custom:
                    {
                        CacheKeyBuilder keyBuilder = null;
                        if (VaryByHeaders?.Length > 0)
                        {
                            keyBuilder = new RequestHeadersCacheKeyBuilder(keyBuilder, strictMode, VaryByHeaders);
                        }
                        if (VaryByClaims?.Length > 0)
                        {
                            keyBuilder = new ClaimsCacheKeyBuilder(keyBuilder, strictMode, VaryByClaims);
                        }
                        if (VaryByQueryKeys?.Length > 0)
                        {
                            keyBuilder = new QueryKeysCacheKeyBuilder(keyBuilder, strictMode, VaryByQueryKeys);
                        }
                        if (VaryByFormKeys?.Length > 0)
                        {
                            keyBuilder = new FormKeysCacheKeyBuilder(keyBuilder, strictMode, VaryByFormKeys);
                        }
                        if (VaryByModels != null)
                        {
                            var modelKeyParserType = ModelKeyParserType ?? typeof(DefaultModelKeyParser);
                            var modelKeyParser = serviceProvider.GetRequiredService(modelKeyParserType) as IModelKeyParser;
                            keyBuilder = new ModelCacheKeyBuilder(keyBuilder, strictMode, VaryByModels, modelKeyParser);
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
                        cacheKeyGenerator = serviceProvider.GetRequiredService<RequestPathCacheKeyGenerator>();
                        break;
                    }

                default:
                    throw new NotImplementedException($"Not ready to support CacheMode: {Mode}");
            }

            return cacheKeyGenerator;
        }

        /// <summary>
        /// 创建Filter
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        private IFilterMetadata CreateFilter(IServiceProvider serviceProvider)
        {
            ILogger GetLogger<T>() => serviceProvider.GetService<ILogger<T>>();

            CheckDuration(Duration);

            var optionsAccessor = serviceProvider.GetRequiredService<IOptions<ResponseCachingOptions>>();
            var options = optionsAccessor.Value;

            IResponseCache responseCache = GetResponseCache(serviceProvider, options);

            ICacheKeyGenerator cacheKeyGenerator = TryGetCustomCacheKeyGenerator(serviceProvider, out FilterType filterType)
                                                   ?? CreateCacheKeyGenerator(serviceProvider, options, out filterType);

            var cacheDeterminer = serviceProvider.GetService<IResponseCacheDeterminer>();

            var lockMode = LockMode == ExecutingLockMode.Default ? options.DefaultExecutingLockMode : LockMode;

            switch (filterType)
            {
                case FilterType.Resource:
                    {
                        Type executingLockerType = lockMode switch
                        {
                            ExecutingLockMode.ActionSingle => typeof(IActionSingleResourceExecutingLocker),
                            ExecutingLockMode.CacheKeySingle => typeof(ICacheKeySingleResourceExecutingLocker),
                            _ => null,
                        };
                        var executingLocker = executingLockerType is null
                                                ? null
                                                : serviceProvider.GetRequiredService(executingLockerType) as IRequestExecutingLocker<ResourceExecutingContext, ResponseCacheEntry>;
                        var responseCachingContext = new ResponseCachingContext<ResourceExecutingContext, ResponseCacheEntry>(this, cacheKeyGenerator, executingLocker, responseCache, cacheDeterminer, optionsAccessor);
                        return new DefaultResourceCacheFilter(responseCachingContext, GetLogger<DefaultResourceCacheFilter>());
                    }
                case FilterType.Action:
                    {
                        Type executingLockerType = lockMode switch
                        {
                            ExecutingLockMode.ActionSingle => typeof(IActionSingleActionExecutingLocker),
                            ExecutingLockMode.CacheKeySingle => typeof(ICacheKeySingleActionExecutingLocker),
                            _ => null,
                        };
                        var executingLocker = executingLockerType is null
                                                ? null
                                                : serviceProvider.GetRequiredService(executingLockerType) as IRequestExecutingLocker<ActionExecutingContext, IActionResult>;
                        var responseCachingContext = new ResponseCachingContext<ActionExecutingContext, IActionResult>(this, cacheKeyGenerator, executingLocker, responseCache, cacheDeterminer, optionsAccessor);
                        return new DefaultActionCacheFilter(responseCachingContext, GetLogger<DefaultActionCacheFilter>());
                    }
                default:
                    throw new NotImplementedException($"Not ready to support FilterType: {filterType}");
            }
        }

        /// <summary>
        /// 获取响应缓存容器
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private IResponseCache GetResponseCache(IServiceProvider serviceProvider, ResponseCachingOptions options)
        {
            var storeLocation = StoreLocation == CacheStoreLocation.Default ? options.DefaultCacheStoreLocation : StoreLocation;

            return storeLocation switch
            {
                CacheStoreLocation.Distributed => serviceProvider.GetRequiredService<IDistributedResponseCache>(),
                CacheStoreLocation.Memory => serviceProvider.GetRequiredService<IMemoryResponseCache>(),
                _ => throw new ArgumentOutOfRangeException($"UnSupport cache location {storeLocation}"),
            };
        }

        /// <summary>
        /// 尝试获取自定义缓存键生成器
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="filterType"></param>
        /// <returns></returns>
        private ICacheKeyGenerator TryGetCustomCacheKeyGenerator(IServiceProvider serviceProvider, out FilterType filterType)
        {
            filterType = FilterType.Resource;
            if (CustomCacheKeyGeneratorType == null)
            {
                return null;
            }

            if (!CustomCacheKeyGeneratorType.IsSubclassOf(typeof(ICustomCacheKeyGenerator)))
            {
                throw new ArgumentException($"type of {CustomCacheKeyGeneratorType} must derives from {nameof(ICustomCacheKeyGenerator)}");
            }

            ICustomCacheKeyGenerator cacheKeyGenerator;
            if (serviceProvider.GetService(CustomCacheKeyGeneratorType) is ICustomCacheKeyGenerator customCacheKeyGenerator)
            {
                cacheKeyGenerator = customCacheKeyGenerator;
                filterType = customCacheKeyGenerator.FilterType;
            }
            else
            {
                throw new ArgumentException($"can not get {CustomCacheKeyGeneratorType} from DI service provider");
            }

            if (cacheKeyGenerator is IResponseCachingAttributeSetter responseCachingAttributeSetter)
            {
                responseCachingAttributeSetter.SetResponseCachingAttribute(this);
            }

            return cacheKeyGenerator;
        }

        #endregion CreateFilter

        #region Internal

        private static void CheckDuration(int duration)
        {
            if (duration < ResponseCachingConstants.MinCacheAvailableSeconds)
            {
                throw new ArgumentOutOfRangeException($"{nameof(duration)} can not less than {ResponseCachingConstants.MinCacheAvailableSeconds} second");
            }
        }

        #endregion Internal
    }
}
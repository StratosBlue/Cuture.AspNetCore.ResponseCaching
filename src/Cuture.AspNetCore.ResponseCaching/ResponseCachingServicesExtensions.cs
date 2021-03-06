﻿using System;
using System.Diagnostics;
using System.Threading;

using Cuture.AspNetCore.ResponseCaching;
using Cuture.AspNetCore.ResponseCaching.CacheKey.Generators;
using Cuture.AspNetCore.ResponseCaching.Diagnostics;
using Cuture.AspNetCore.ResponseCaching.Interceptors;
using Cuture.AspNetCore.ResponseCaching.Internal;
using Cuture.AspNetCore.ResponseCaching.Lockers;
using Cuture.AspNetCore.ResponseCaching.ResponseCaches;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///
    /// </summary>
    public static class ResponseCachingServicesExtensions
    {
        #region AddCaching

        /// <summary>
        /// 添加响应缓存
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static ResponseCachingBuilder AddCaching(this IServiceCollection services)
        {
            services.AddOptions<InterceptorOptions>();

            services.AddOptions<ResponseCachingExecutingLockOptions>().PostConfigure(options => options.CheckOptions());

            services.TryAddSingleton<IResponseCachingFilterBuilder, DefaultResponseCachingFilterBuilder>();

            services.TryAddSingleton<IMemoryResponseCache, DefaultMemoryResponseCache>();

            services.TryAddSingleton<FullPathAndQueryCacheKeyGenerator>();
            services.TryAddSingleton<RequestPathCacheKeyGenerator>();
            services.TryAddSingleton<DefaultModelKeyParser>();

            services.TryAddSingleton<IResponseCacheDeterminer, DefaultResponseCacheDeterminer>();

            services.TryAddSingleton<IExecutingLockerProvider, DefaultExecutingLockerProvider>();

            services.TryAddSingleton(services =>
            {
                var options = services.GetRequiredService<IOptions<ResponseCachingExecutingLockOptions>>().Value;

                var boundedObjectPoolOptions = new BoundedObjectPoolOptions()
                {
                    MaximumPooled = options.MaximumSemaphorePooled,
                    MinimumRetained = options.MinimumSemaphoreRetained,
                    RecycleInterval = options.SemaphoreRecycleInterval,
                };

                var semaphorePool = BoundedObjectPool.Create(new SinglePassSemaphoreLifecycleExecutor(), boundedObjectPoolOptions);

                return (INakedBoundedObjectPool<SemaphoreSlim>)semaphorePool;
            });

            services.TryAddSingleton(typeof(ExecutionLockStateLifecycleExecutor<>));

            services.TryAddSingleton(services => CreateLockStatePool<IActionResult>(services));
            services.TryAddSingleton(services => CreateLockStatePool<ResponseCacheEntry>(services));

            services.TryAddSingleton(serviceProvider => new CachingDiagnostics(serviceProvider));
            services.TryAddSingleton<CachingDiagnosticsAccessor>();

            services.TryAddSingleton<IHotDataCacheProvider, DefaultHotDataCacheProvider>();

            services.AddHttpContextAccessor();

            return new ResponseCachingBuilder(services);
        }

        /// <summary>
        /// 添加响应缓存
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static ResponseCachingBuilder AddCaching(this IServiceCollection services, Action<ResponseCachingOptions> configureOptions)
        {
            services.AddOptions<ResponseCachingOptions>().PostConfigure(configureOptions);
            return services.AddCaching();
        }

        /// <summary>
        /// 添加响应缓存
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static ResponseCachingBuilder AddCaching(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<ResponseCachingOptions>().Bind(configuration);
            return services.AddCaching();
        }

        private static INakedBoundedObjectPool<ExecutionLockState<TPayload>> CreateLockStatePool<TPayload>(IServiceProvider services) where TPayload : class
        {
            var options = services.GetRequiredService<IOptions<ResponseCachingExecutingLockOptions>>().Value;

            var boundedObjectPoolOptions = new BoundedObjectPoolOptions()
            {
                MaximumPooled = options.MaximumLockStatePooled,
                MinimumRetained = options.MinimumLockStateRetained,
                RecycleInterval = options.LockStateRecycleInterval,
            };

            var lifecycleExecutor = services.GetRequiredService<ExecutionLockStateLifecycleExecutor<TPayload>>();
            var pool = BoundedObjectPool.Create(lifecycleExecutor, boundedObjectPoolOptions);
            return (INakedBoundedObjectPool<ExecutionLockState<TPayload>>)pool;
        }

        #endregion AddCaching

        #region Interceptor

        /// <summary>
        /// 配置全局拦截器
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static ResponseCachingBuilder ConfigureInterceptor(this ResponseCachingBuilder builder, Action<InterceptorOptions> configureOptions)
        {
            builder.Services.PostConfigure(configureOptions);
            return builder;
        }

        /// <summary>
        /// 使用缓存命中标记响应头（在命中缓存时的响应头中增加标记）
        /// <para/>
        /// Note!!!
        /// <para/>
        /// * 此设置将会覆盖之前对<see cref="InterceptorOptions.CachingProcessInterceptorType"/>的设置
        /// <para/>
        /// * 对<see cref="InterceptorOptions.CachingProcessInterceptorType"/>的重新设置也会使此设置失效
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ResponseCachingBuilder UseCacheHitStampHeader(this ResponseCachingBuilder builder, string key, string value)
        {
            builder.ConfigureInterceptor(options =>
            {
                options.CachingProcessInterceptorType = typeof(CacheHitStampCachingProcessInterceptor);
            });

            var interceptor = new CacheHitStampCachingProcessInterceptor(key, value);

            builder.Services.AddSingleton<CacheHitStampCachingProcessInterceptor>(interceptor);

            return builder;
        }

        #endregion Interceptor

        #region Diagnostics

        #region services

        /// <summary>
        /// 添加Debug模式下的诊断信息日志输出
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ResponseCachingBuilder AddDiagnosticDebugLogger(this ResponseCachingBuilder builder)
        {
            builder.InternalAddDiagnosticDebugLogger();
            return builder;
        }

        /// <summary>
        /// 添加Release模式下的诊断信息日志输出
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ResponseCachingBuilder AddDiagnosticReleaseLogger(this ResponseCachingBuilder builder)
        {
            builder.InternalAddDiagnosticReleaseLogger();
            return builder;
        }

        [Conditional("DEBUG")]
        internal static void InternalAddDiagnosticDebugLogger(this ResponseCachingBuilder builder)
        {
            builder.InternalAddDiagnosticLogger();
        }

        internal static void InternalAddDiagnosticLogger(this ResponseCachingBuilder builder)
        {
            var services = builder.Services;

            var diagnosticsDescriptor = ServiceDescriptor.Singleton(serviceProvider => new CachingDiagnostics(serviceProvider, new DiagnosticListener(ResponseCachingEventData.DiagnosticName)));
            services.Replace(diagnosticsDescriptor);

            services.TryAddSingleton(serviceProvider => new DiagnosticLogger(serviceProvider));
            services.TryAddSingleton(serviceProvider => new DiagnosticLoggerSubscriber(serviceProvider));
            services.TryAddSingleton(new DiagnosticLoggerSubscriberDisposerAccessor());
        }

        [Conditional("RELEASE")]
        internal static void InternalAddDiagnosticReleaseLogger(this ResponseCachingBuilder builder)
        {
            builder.InternalAddDiagnosticLogger();
        }

        #endregion services

        #region initialization

        /// <summary>
        /// 启用缓存诊断日志
        /// </summary>
        /// <param name="builder"></param>
        public static void EnableResponseCachingDiagnosticLogger(this IApplicationBuilder builder)
        {
            builder.ApplicationServices.EnableResponseCachingDiagnosticLogger();
        }

        /// <summary>
        /// 启用缓存诊断日志
        /// </summary>
        /// <param name="serviceProvider"></param>
        public static void EnableResponseCachingDiagnosticLogger(this IServiceProvider serviceProvider)
        {
            var diagnosticLoggerSubscriber = serviceProvider.GetService<DiagnosticLoggerSubscriber>();
            var diagnosticLoggerSubscriberDisposerAccessor = serviceProvider.GetService<DiagnosticLoggerSubscriberDisposerAccessor>();

            if (diagnosticLoggerSubscriber is null
                || diagnosticLoggerSubscriberDisposerAccessor is null)
            {
                return;
                //throw new ResponseCachingException($"Must Add Diagnostic Logger Into {nameof(IServiceCollection)} Before Enable ResponseCaching Diagnostic Logger.");
            }

            var disposable = DiagnosticListener.AllListeners.Subscribe(diagnosticLoggerSubscriber);

            diagnosticLoggerSubscriberDisposerAccessor.Disposable = disposable;
        }

        #endregion initialization

        #endregion Diagnostics
    }
}
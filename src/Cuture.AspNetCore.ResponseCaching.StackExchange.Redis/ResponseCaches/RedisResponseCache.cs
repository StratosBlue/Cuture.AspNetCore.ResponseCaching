﻿using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using StackExchange.Redis;
using StackExchange.Redis.KeyspaceIsolation;

namespace Cuture.AspNetCore.ResponseCaching.ResponseCaches
{
    /// <summary>
    /// 基于 <see cref="StackExchange.Redis"/> 的响应缓存
    /// </summary>
    public class RedisResponseCache : IDistributedResponseCache
    {
        #region const

        /// <summary>
        /// BodyFieldName Hash字段名称
        /// </summary>
        public const string BodyFieldName = "Body";

        /// <summary>
        /// ContentType Hash字段名称
        /// </summary>
        public const string ContentTypeFieldName = "ContentType";

        /// <summary>
        /// Expire Hash字段名称
        /// </summary>
        public const string ExpireFieldName = "Expire";

        #endregion const

        #region Private 字段

        private static readonly RedisValue[] _fieldNames = new RedisValue[] { ContentTypeFieldName, BodyFieldName, ExpireFieldName };

        private readonly RedisValue _bodyFieldName = BodyFieldName;
        private readonly RedisValue _contenTypeFieldName = ContentTypeFieldName;
        private readonly IDatabase _database;
        private readonly RedisValue _expireFieldName = ExpireFieldName;

        #endregion Private 字段

        #region Public 构造函数

        /// <summary>
        /// 基于 <see cref="StackExchange.Redis"/> 的响应缓存
        /// </summary>
        /// <param name="optionAccessor">缓存选项</param>
        public RedisResponseCache(IOptions<RedisResponseCacheOptions> optionAccessor)
        {
            var options = optionAccessor.Value;
            _database = options.ConnectionMultiplexer!.GetDatabase();
            var prefix = options.CacheKeyPrefix;
            if (!string.IsNullOrEmpty(prefix))
            {
                _database = _database.WithKeyPrefix(prefix);
            }
        }

        #endregion Public 构造函数

        #region Public 方法

        /// <inheritdoc/>
        public async Task<ResponseCacheEntry?> GetAsync(string key)
        {
            var redisValues = await _database.HashGetAsync(key, _fieldNames);
            if (redisValues[0].IsNull || redisValues[1].IsNull || redisValues[2].IsNull)
            {
                return null;
            }
            var expire = (long)redisValues[2];
            if (expire < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return null;
            }
            return new ResponseCacheEntry(redisValues[0], redisValues[1], expire);
        }

        /// <inheritdoc/>
        public async Task SetAsync(string key, ResponseCacheEntry entry)
        {
            RedisKey redisKey = key;
            await _database.HashSetAsync(redisKey, new[] {
                new HashEntry(_contenTypeFieldName, entry.ContentType),
                new HashEntry(_bodyFieldName, entry.Body),
                new HashEntry(_expireFieldName, entry.Expire),
            });
            _ = _database.KeyExpireAsync(redisKey, DateTimeOffset.FromUnixTimeMilliseconds(entry.Expire).UtcDateTime);
        }

        #endregion Public 方法
    }
}
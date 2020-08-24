﻿using System;
using System.Threading.Tasks;

using Cuture.Http;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using ResponseCaching.Test.Base;
using ResponseCaching.Test.WebHost.Models;

namespace ResponseCaching.Test.RequestTests
{
    /// <summary>
    /// 没有缓存的接口请求测试
    /// </summary>
    [TestClass]
    public class NoneCachingTest : BaseRequestTest
    {
        protected override Func<Task<TextHttpOperationResult<WeatherForecast[]>>>[] GetAllRequestFuncs()
        {
            return new Func<Task<TextHttpOperationResult<WeatherForecast[]>>>[] {
                () => $"{BaseUrl}/WeatherForecast/get?page=1&pageSize=5".ToHttpRequest().TryGetAsObjectAsync<WeatherForecast[]>(),
                () => $"{BaseUrl}/WeatherForecast/get?page=1&pageSize=6".ToHttpRequest().TryGetAsObjectAsync<WeatherForecast[]>(),
                () => $"{BaseUrl}/WeatherForecast/get?page=2&pageSize=4".ToHttpRequest().TryGetAsObjectAsync<WeatherForecast[]>(),
                () => $"{BaseUrl}/WeatherForecast/get?page=2&pageSize=6".ToHttpRequest().TryGetAsObjectAsync<WeatherForecast[]>(),
                () => $"{BaseUrl}/WeatherForecast/get?page=3&pageSize=3".ToHttpRequest().TryGetAsObjectAsync<WeatherForecast[]>(),
            };
        }

        [TestMethod]
        public override async Task ExecuteAsync()
        {
            var funcs = GetAllRequestFuncs();

            for (int time = 0; time < 3; time++)
            {
                var data = await IntervalRunAsync(funcs);

                Assert.IsTrue(data.Length > 0);

                for (int i = 0; i < data.Length - 1; i++)
                {
                    for (int j = i + 1; j < data.Length; j++)
                    {
                        Assert.AreNotEqual(data[i], data[j]);
                    }
                }
            }
        }
    }
}
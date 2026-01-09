using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using ADOApi.Services;
using Xunit;

public class CachingServiceTests
{
    [Fact]
    public async Task GetOrSetAsync_CachesValue_ReducesFactoryCalls()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<CachingService>();
        var cachingService = new CachingService(memoryCache, logger);

        int callCount = 0;
        Func<Task<string>> factory = async () =>
        {
            await Task.Delay(1);
            callCount++;
            return "value";
        };

        var v1 = await cachingService.GetOrSetAsync<string>("key1", factory, TimeSpan.FromMinutes(5));
        var v2 = await cachingService.GetOrSetAsync<string>("key1", factory, TimeSpan.FromMinutes(5));

        Assert.Equal("value", v1);
        Assert.Equal("value", v2);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GetOrSetAsync_DistinctKeys_CallFactoryForEachKey()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<CachingService>();
        var cachingService = new CachingService(memoryCache, logger);

        int callCount = 0;
        Func<Task<string>> factory = async () =>
        {
            await Task.Delay(1);
            callCount++;
            return "value";
        };

        var v1 = await cachingService.GetOrSetAsync<string>("key-a", factory, TimeSpan.FromMinutes(5));
        var v2 = await cachingService.GetOrSetAsync<string>("key-b", factory, TimeSpan.FromMinutes(5));

        Assert.Equal(2, callCount);
    }
}

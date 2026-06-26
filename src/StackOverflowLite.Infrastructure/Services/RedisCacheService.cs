using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackOverflowLite.Application.Interfaces;

namespace StackOverflowLite.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly StackExchange.Redis.IConnectionMultiplexer _multiplexer;

    public RedisCacheService(IDistributedCache cache, StackExchange.Redis.IConnectionMultiplexer multiplexer)
    {
        _cache = cache;
        _multiplexer = multiplexer;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var cachedString = await _cache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrEmpty(cachedString))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(cachedString);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpireTime = null, CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions();
        if (absoluteExpireTime.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = absoluteExpireTime;
        }

        var serializedValue = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, serializedValue, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }

    public async Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default)
    {
        var endpoints = _multiplexer.GetEndPoints();
        var server = _multiplexer.GetServer(endpoints.First());
        
        // StackExchangeRedisCache prepends the InstanceName to keys.
        // Our instance name is "StackOverflowLite_"
        var pattern = $"StackOverflowLite_{prefixKey}*";
        var keys = server.Keys(pattern: pattern).ToArray();
        
        if (keys.Length > 0)
        {
            var db = _multiplexer.GetDatabase();
            await db.KeyDeleteAsync(keys);
        }
    }
}

using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using PharmaGo.Application.Abstractions;

namespace PharmaGo.Infrastructure.Caching;

public class DistributedAppCacheService(IDistributedCache cache) : IAppCacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var payload = await cache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(payload, SerializerOptions);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        return cache.SetStringAsync(key, payload, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        }, cancellationToken);
    }

    public async Task<string> GetScopeVersionAsync(string scope, CancellationToken cancellationToken = default)
    {
        var key = BuildScopeVersionKey(scope);
        var currentVersion = await cache.GetStringAsync(key, cancellationToken);
        if (!string.IsNullOrWhiteSpace(currentVersion))
        {
            return currentVersion;
        }

        const string initialVersion = "1";
        await cache.SetStringAsync(key, initialVersion, cancellationToken);
        return initialVersion;
    }

    public async Task BumpScopeVersionAsync(string scope, CancellationToken cancellationToken = default)
    {
        var key = BuildScopeVersionKey(scope);
        var currentVersion = await GetScopeVersionAsync(scope, cancellationToken);
        var parsed = long.TryParse(currentVersion, out var version) ? version : 1;
        await cache.SetStringAsync(key, (parsed + 1).ToString(), cancellationToken);
    }

    private static string BuildScopeVersionKey(string scope) => $"cache-scope:{scope}:version";
}

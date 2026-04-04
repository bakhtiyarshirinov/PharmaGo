namespace PharmaGo.Application.Abstractions;

public interface IAppCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<string> GetScopeVersionAsync(string scope, CancellationToken cancellationToken = default);
    Task BumpScopeVersionAsync(string scope, CancellationToken cancellationToken = default);
}

using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.MinimalApis.ApiCaching.Local;

internal sealed class LocalApiCachingFilter(
    IMemoryCache cache,
    LocalApiCachingOptions options
) : ApiCachingFilterBase(options)
{
    private readonly IMemoryCache _cache = cache;

    public override Task<byte[]?> GetFromCacheAsync(
        string cacheKey,
        CancellationToken cancellationToken
    )
    {
        _cache.TryGetValue(cacheKey, out byte[]? value);
        return Task.FromResult(value);
    }

    public override Task AddToCacheAsync(
        string cacheKey,
        byte[] value,
        TimeSpan expiration,
        CancellationToken cancellationToken
    )
    {
        using var cacheEntry = _cache.CreateEntry(cacheKey);
        cacheEntry.SetValue(value);
        cacheEntry.SetOptions(new()
        {
            AbsoluteExpirationRelativeToNow = expiration
        });

        return Task.CompletedTask;
    }
}

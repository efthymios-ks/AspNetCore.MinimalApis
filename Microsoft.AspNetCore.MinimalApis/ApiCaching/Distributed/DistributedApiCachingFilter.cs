using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.AspNetCore.MinimalApis.ApiCaching.Distributed;

internal sealed class DistributedApiCachingFilter(
    IDistributedCache cache,
    DistributedApiCachingOptions options
    ) : ApiCachingFilterBase(options)
{
    private readonly IDistributedCache _cache = cache;

    public override async Task<byte[]?> GetFromCacheAsync(
        string cacheKey,
        CancellationToken cancellationToken
    ) => await _cache.GetAsync(cacheKey, cancellationToken);

    public override async Task AddToCacheAsync(
        string cacheKey,
        byte[] value,
        TimeSpan expiration,
        CancellationToken cancellationToken
    ) => await _cache.SetAsync(cacheKey, value, new()
    {
        AbsoluteExpirationRelativeToNow = expiration
    }, cancellationToken);
}

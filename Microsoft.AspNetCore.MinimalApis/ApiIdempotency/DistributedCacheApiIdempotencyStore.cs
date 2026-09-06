using Microsoft.AspNetCore.MinimalApis.Utilities;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Microsoft.AspNetCore.MinimalApis.ApiIdempotency;

public sealed class DistributedCacheApiIdempotencyStore(IDistributedCache cache)
    : IApiIdempotencyStore
{
    private static readonly byte[] _pendingMarker = [0];
    private readonly IDistributedCache _cache = cache;

    public async Task<IdempotencyEntry> TryReserveAsync(
        string key,
        TimeSpan ttl,
        CancellationToken cancellationToken = default
    )
    {
        var existing = await _cache.GetAsync(key, cancellationToken);
        if (existing is not null)
        {
            return IsPendingMarker(existing)
                ? IdempotencyEntry.Pending
                : Resolve(existing);
        }

        await _cache.SetAsync(key, _pendingMarker, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        }, cancellationToken);

        return IdempotencyEntry.Reserved;
    }

    public Task CompleteAsync(
        string key,
        CachedResponse response,
        TimeSpan ttl,
        CancellationToken cancellationToken = default
    ) => _cache.SetAsync(
        key,
        JsonSerializer.SerializeToUtf8Bytes(response),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
        cancellationToken
    );

    public Task ReleaseAsync(
        string key,
        CancellationToken cancellationToken = default
    ) => _cache.RemoveAsync(key, cancellationToken);

    private static bool IsPendingMarker(byte[] value)
        => value.Length == _pendingMarker.Length && value[0] == _pendingMarker[0];

    private static IdempotencyEntry Resolve(byte[] value)
    {
        try
        {
            var response = JsonSerializer.Deserialize<CachedResponse>(value);
            return response is null
                ? IdempotencyEntry.Pending
                : IdempotencyEntry.Completed(response);
        }
        catch (JsonException)
        {
            // Corrupt entry — treat as in-flight; its TTL will clear it.
            return IdempotencyEntry.Pending;
        }
    }
}

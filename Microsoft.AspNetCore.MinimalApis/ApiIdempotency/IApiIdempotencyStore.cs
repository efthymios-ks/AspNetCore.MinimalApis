using Microsoft.AspNetCore.MinimalApis.Utilities;

namespace Microsoft.AspNetCore.MinimalApis.ApiIdempotency;

public interface IApiIdempotencyStore
{
    Task<IdempotencyEntry> TryReserveAsync(
        string key,
        TimeSpan ttl,
        CancellationToken cancellationToken = default
    );

    Task CompleteAsync(
        string key,
        CachedResponse response,
        TimeSpan ttl,
        CancellationToken cancellationToken = default
    );

    Task ReleaseAsync(
        string key,
        CancellationToken cancellationToken = default
    );
}

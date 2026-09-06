using Microsoft.AspNetCore.MinimalApis.Utilities;

namespace Microsoft.AspNetCore.MinimalApis.ApiIdempotency;

public sealed record IdempotencyEntry(
    IdempotencyStatus Status,
    CachedResponse? Response
)
{
    public static IdempotencyEntry Reserved { get; } = new(IdempotencyStatus.Reserved, null);

    public static IdempotencyEntry Pending { get; } = new(IdempotencyStatus.Pending, null);

    public static IdempotencyEntry Completed(CachedResponse response)
        => new(IdempotencyStatus.Completed, response);
}

using Microsoft.AspNetCore.MinimalApis.ApiIdempotency;
using Microsoft.AspNetCore.MinimalApis.Utilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiIdempotency;

public sealed class DistributedCacheApiIdempotencyStoreTests
{
    private static readonly TimeSpan _ttl = TimeSpan.FromMinutes(1);
    private readonly MemoryDistributedCache _cache = new(Options.Create(new MemoryDistributedCacheOptions()));

    [Fact]
    public async Task TryReserveAsync_WhenKeyIsFree_ShouldReturnReserved()
    {
        // Arrange
        var store = new DistributedCacheApiIdempotencyStore(_cache);

        // Act
        var entry = await store.TryReserveAsync("key", _ttl);

        // Assert
        Assert.Equal(IdempotencyStatus.Reserved, entry.Status);
    }

    [Fact]
    public async Task TryReserveAsync_WhenAlreadyReserved_ShouldReturnPending()
    {
        // Arrange
        var store = new DistributedCacheApiIdempotencyStore(_cache);
        await store.TryReserveAsync("key", _ttl);

        // Act
        var entry = await store.TryReserveAsync("key", _ttl);

        // Assert
        Assert.Equal(IdempotencyStatus.Pending, entry.Status);
    }

    [Fact]
    public async Task TryReserveAsync_WhenCompleted_ShouldReturnCompletedWithDeserializedResponse()
    {
        // Arrange
        var store = new DistributedCacheApiIdempotencyStore(_cache);
        var response = new CachedResponse(201, "application/json", "body"u8.ToArray());
        await store.TryReserveAsync("key", _ttl);
        await store.CompleteAsync("key", response, _ttl);

        // Act
        var entry = await store.TryReserveAsync("key", _ttl);

        // Assert
        Assert.Equal(IdempotencyStatus.Completed, entry.Status);
        Assert.Equal(response.StatusCode, entry.Response!.StatusCode);
        Assert.Equal(response.ContentType, entry.Response.ContentType);
        Assert.Equal(response.Body, entry.Response.Body);
    }

    [Fact]
    public async Task ReleaseAsync_WhenCalled_ShouldFreeTheKey()
    {
        // Arrange
        var store = new DistributedCacheApiIdempotencyStore(_cache);
        await store.TryReserveAsync("key", _ttl);

        // Act
        await store.ReleaseAsync("key");
        var entry = await store.TryReserveAsync("key", _ttl);

        // Assert
        Assert.Equal(IdempotencyStatus.Reserved, entry.Status);
    }

    [Fact]
    public async Task TryReserveAsync_WhenEntryIsCorrupt_ShouldTreatAsPending()
    {
        // Arrange — a non-marker, non-JSON value simulates a corrupt/incompatible entry.
        await _cache.SetAsync("key", [1, 2, 3], new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _ttl
        });
        var store = new DistributedCacheApiIdempotencyStore(_cache);

        // Act
        var entry = await store.TryReserveAsync("key", _ttl);

        // Assert
        Assert.Equal(IdempotencyStatus.Pending, entry.Status);
    }
}

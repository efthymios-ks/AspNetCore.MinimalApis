using Microsoft.AspNetCore.MinimalApis.ApiCaching.Local;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiCaching.Local;

public sealed class LocalApiCachingFilterTests
{
    private readonly IMemoryCache _cache = Substitute.For<IMemoryCache>();

    [Fact]
    public async Task GetFromCacheAsync_WhenCacheHit_ShouldReturnValue()
    {
        // Arrange
        var expected = new byte[] { 1, 2, 3 };
        object? cacheValue = expected;
        _cache.TryGetValue("key", out Arg.Any<byte[]?>())
            .Returns(caller =>
            {
                caller[1] = expected;
                return true;
            });
        var filter = new LocalApiCachingFilter(_cache, new LocalApiCachingOptions());

        // Act
        var result = await filter.GetFromCacheAsync("key", CancellationToken.None);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetFromCacheAsync_WhenCacheMiss_ShouldReturnNull()
    {
        // Arrange
        _cache.TryGetValue(default!, out Arg.Any<object?>())
            .ReturnsForAnyArgs(false);
        var filter = new LocalApiCachingFilter(_cache, new LocalApiCachingOptions());

        // Act
        var result = await filter.GetFromCacheAsync("missing-key", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddToCacheAsync_WhenCalled_ShouldCreateEntryAndSetValueAndExpiry()
    {
        // Arrange
        var cacheEntry = Substitute.For<ICacheEntry>();
        _cache.CreateEntry("key")
            .Returns(cacheEntry);
        var filter = new LocalApiCachingFilter(_cache, new LocalApiCachingOptions());
        var value = new byte[] { 10, 20 };
        var expiry = TimeSpan.FromMinutes(5);

        // Act
        await filter.AddToCacheAsync("key", value, expiry, CancellationToken.None);

        // Assert
        _cache.Received(1).CreateEntry("key");
        cacheEntry.Received(1).SetValue(value);
    }
}

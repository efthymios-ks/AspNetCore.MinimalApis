using Microsoft.AspNetCore.MinimalApis.ApiCaching.Distributed;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiCaching.Distributed;

public sealed class DistributedApiCachingFilterTests
{
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();

    [Fact]
    public async Task GetFromCacheAsync_WhenCacheHit_ShouldReturnValue()
    {
        // Arrange
        var expected = new byte[] { 1, 2, 3 };
        _cache.GetAsync(default!, default)
            .ReturnsForAnyArgs(expected);
        var filter = new DistributedApiCachingFilter(_cache, new DistributedApiCachingOptions());

        // Act
        var result = await filter.GetFromCacheAsync("key", CancellationToken.None);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetFromCacheAsync_WhenCacheMiss_ShouldReturnNull()
    {
        // Arrange
        _cache.GetAsync(default!, default)
            .ReturnsForAnyArgs((byte[]?)null);
        var filter = new DistributedApiCachingFilter(_cache, new DistributedApiCachingOptions());

        // Act
        var result = await filter.GetFromCacheAsync("missing-key", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddToCacheAsync_WhenCalled_ShouldCallCacheSet()
    {
        // Arrange
        var filter = new DistributedApiCachingFilter(_cache, new DistributedApiCachingOptions());
        var value = new byte[] { 10, 20 };
        var expiry = TimeSpan.FromMinutes(5);

        // Act
        await filter.AddToCacheAsync("key", value, expiry, CancellationToken.None);

        // Assert
        await _cache.ReceivedWithAnyArgs(1)
            .SetAsync(default!, default!, default!, default);
    }
}

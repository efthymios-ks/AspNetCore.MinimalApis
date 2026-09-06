using Microsoft.AspNetCore.MinimalApis.Broadcast;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using StackExchange.Redis;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.Broadcast;

public sealed class BroadcastExtensionsTests
{
    [Fact]
    public void AddBroadcaster_ShouldRegisterInProcessTransportBroadcasterAndTimeProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBroadcaster();

        // Assert
        var provider = services.BuildServiceProvider();
        Assert.IsType<Broadcaster>(provider.GetRequiredService<IBroadcaster>());
        Assert.IsType<InProcessBroadcast>(provider.GetRequiredService<IBroadcastTransport>());
        Assert.NotNull(provider.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public void AddRedisBroadcaster_ShouldRegisterRedisTransport()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IConnectionMultiplexer>());

        // Act
        services.AddRedisBroadcaster("orders:");

        // Assert
        var provider = services.BuildServiceProvider();
        Assert.IsType<RedisBroadcast>(provider.GetRequiredService<IBroadcastTransport>());
    }
}

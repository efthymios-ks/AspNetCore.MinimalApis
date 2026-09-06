using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Microsoft.AspNetCore.MinimalApis.Broadcast;

public static class RedisBroadcastExtensions
{
    /// <summary>
    /// Redis pub/sub transport, for multi-pod scenarios. Requires an IConnectionMultiplexer in DI,
    /// </summary>
    public static IServiceCollection AddRedisBroadcaster(
        this IServiceCollection services,
        string keyPrefix
    ) => services.AddBroadcaster(serviceProvider
            => new RedisBroadcast(serviceProvider.GetRequiredService<IConnectionMultiplexer>(), keyPrefix));
}

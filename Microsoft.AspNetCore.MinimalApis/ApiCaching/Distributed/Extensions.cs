using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.MinimalApis.ApiCaching.Distributed;

public static class Extensions
{
    public static IServiceCollection AddDistributedApiCaching(
        this IServiceCollection services,
        Action<DistributedApiCachingOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        configure ??= _ => { };
        services.Configure(configure);
        return services;
    }

    public static RouteHandlerBuilder WithDistributedApiCaching(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithDistributedApiCachingInternal(services
            => services.GetRequiredService<IOptions<DistributedApiCachingOptions>>().Value
        );
    }

    public static RouteHandlerBuilder WithDistributedApiCaching(
        this RouteHandlerBuilder builder,
        Action<DistributedApiCachingOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.WithDistributedApiCachingInternal(_ =>
        {
            var options = new DistributedApiCachingOptions();
            configure(options);
            return options;
        });
    }

    private static RouteHandlerBuilder WithDistributedApiCachingInternal(
        this RouteHandlerBuilder builder,
        Func<IServiceProvider, DistributedApiCachingOptions> optionsProvider
    )
    {
        builder.Add(endpointBuilder =>
        {
            endpointBuilder.FilterFactories.Add((context, next) =>
            {
                var services = context.ApplicationServices;
                var cache = services.GetRequiredService<IDistributedCache>();
                var options = optionsProvider(services);
                var filter = new DistributedApiCachingFilter(cache, options);
                return invocationContext => filter.InvokeAsync(invocationContext, next);
            });
        });

        return builder;
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.MinimalApis.ApiCaching.Local;

public static class Extensions
{
    public static IServiceCollection AddLocalApiCaching(
        this IServiceCollection services,
        Action<LocalApiCachingOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        configure ??= _ => { };

        services.AddMemoryCache();
        services.Configure(configure);

        return services;
    }

    public static RouteHandlerBuilder WithLocalApiCaching(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithLocalApiCachingInternal(services
            => services.GetRequiredService<IOptions<LocalApiCachingOptions>>().Value
        );
    }

    public static RouteHandlerBuilder WithLocalApiCaching(
        this RouteHandlerBuilder builder,
        Action<LocalApiCachingOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.WithLocalApiCachingInternal(_ =>
        {
            var options = new LocalApiCachingOptions();
            configure(options);
            return options;
        });
    }

    private static RouteHandlerBuilder WithLocalApiCachingInternal(
        this RouteHandlerBuilder builder,
        Func<IServiceProvider, LocalApiCachingOptions> optionsProvider
    )
    {
        builder.Add(endpointBuilder =>
        {
            endpointBuilder.FilterFactories.Add((context, next) =>
            {
                var services = context.ApplicationServices;
                var cache = services.GetRequiredService<IMemoryCache>();
                var options = optionsProvider(services);
                var filter = new LocalApiCachingFilter(cache, options);
                return invocationContext => filter.InvokeAsync(invocationContext, next);
            });
        });

        return builder;
    }
}

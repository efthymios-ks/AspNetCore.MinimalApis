using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.MinimalApis.ApiIdempotency;

public static class DependencyInjection
{
    /// <summary>
    /// Registers idempotency using the default <see cref="DistributedCacheApiIdempotencyStore"/><br/>
    /// (requires a registered <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/>).
    /// </summary>
    public static IServiceCollection AddApiIdempotency(
        this IServiceCollection services,
        Action<ApiIdempotencyOptions>? configure = null
    ) => services.AddApiIdempotency<DistributedCacheApiIdempotencyStore>(configure);

    /// <summary>
    /// Registers idempotency using a custom <see cref="IApiIdempotencyStore"/>.
    /// </summary>
    public static IServiceCollection AddApiIdempotency<TIdempotencyStore>(
        this IServiceCollection services,
        Action<ApiIdempotencyOptions>? configure = null
    ) where TIdempotencyStore : class, IApiIdempotencyStore
    {
        ArgumentNullException.ThrowIfNull(services);

        configure ??= _ => { };

        services.TryAddSingleton<IApiIdempotencyStore, TIdempotencyStore>();
        services.Configure(configure);

        return services;
    }
    public static RouteHandlerBuilder WithApiIdempotency(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithApiIdempotencyInternal(services
            => services.GetRequiredService<IOptions<ApiIdempotencyOptions>>().Value
        );
    }

    public static RouteHandlerBuilder WithApiIdempotency(
        this RouteHandlerBuilder builder,
        Action<ApiIdempotencyOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.WithApiIdempotencyInternal(_ =>
        {
            var options = new ApiIdempotencyOptions();
            configure(options);
            return options;
        });
    }

    private static RouteHandlerBuilder WithApiIdempotencyInternal(
        this RouteHandlerBuilder builder,
        Func<IServiceProvider, ApiIdempotencyOptions> optionsProvider
    )
    {
        builder.Add(endpointBuilder =>
        {
            endpointBuilder.FilterFactories.Add((context, next) =>
            {
                var logger = context.ApplicationServices.GetRequiredService<ILogger<ApiIdempotencyFilter>>();
                var options = Options.Create(optionsProvider(context.ApplicationServices));

                // Resolve the store when the filter actually runs, so a missing dependency
                // surfaces an insightful error only on idempotency-enabled requests.
                return invocationContext =>
                {
                    var store = ResolveStore(invocationContext.HttpContext.RequestServices);
                    var filter = new ApiIdempotencyFilter(store, logger, options);
                    return filter.InvokeAsync(invocationContext, next);
                };
            });
        });

        return builder;
    }

    private static IApiIdempotencyStore ResolveStore(IServiceProvider services)
    {
        try
        {
            return services.GetRequiredService<IApiIdempotencyStore>();
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidOperationException(
                "API idempotency is enabled on this endpoint but its store could not be resolved. " +
                "Ensure AddApiIdempotency() is called at startup and that the default DistributedCacheApiIdempotencyStore " +
                "has an IDistributedCache registered (e.g. AddStackExchangeRedisCache(...) or " +
                "AddDistributedMemoryCache()). To use a different backing store, register it via " +
                "AddApiIdempotency<TStore>().",
                exception
            );
        }
    }
}

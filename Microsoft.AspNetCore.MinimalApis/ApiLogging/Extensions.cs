using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.MinimalApis.ApiLogging;

public static class Extensions
{
    public static IServiceCollection AddApiRequestLog(
        this IServiceCollection services,
        Action<GlobalLogScopeOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        configure ??= _ => { };
        services.Configure(configure);

        return services;
    }

    public static IApplicationBuilder UseApiRequestLog(
        this IApplicationBuilder app
    )
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<LogRequestMiddleware>();
    }

    public static RouteHandlerBuilder WithAdditionalLogProperties(
        this RouteHandlerBuilder builder,
        Action<EndpointLogScopeOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.FilterFactories.Add((_, next) =>
            {
                return async invocationContext =>
                {
                    var localOptions = new EndpointLogScopeOptions();
                    configure(localOptions);

                    var localProperties = localOptions.PropertiesSelector(invocationContext);
                    if (localProperties is not null && localProperties.Count > 0)
                    {
                        var existingProperties = invocationContext
                            .HttpContext
                            .Items[EndpointLogScopeOptions.Key] as Dictionary<string, object?>
                            ?? [];

                        foreach (var (key, value) in localProperties)
                        {
                            existingProperties[key] = value;
                        }

                        invocationContext.HttpContext.Items[EndpointLogScopeOptions.Key] = existingProperties;
                    }

                    return await next(invocationContext);
                };
            });
        });

        return builder;
    }
}

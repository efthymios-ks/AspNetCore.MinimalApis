using Asp.Versioning;
using Microsoft.AspNetCore.MinimalApis.ApiSwagger;
using Microsoft.AspNetCore.MinimalApis.ApiValidators;
using Microsoft.AspNetCore.MinimalApis.ApiVersions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Microsoft.AspNetCore.MinimalApis.ApiEndpoints;

public static class Extensions
{
    public static IServiceCollection AddApiEndpoints(this IServiceCollection services)
        => services.AddApiEndpoints(Assembly.GetEntryAssembly()!);

    public static IServiceCollection AddApiEndpoints(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        var endpointTypes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type
                => type.IsClass && !type.IsAbstract && !type.IsInterface
                && type.IsAssignableTo(typeof(ApiEndpoint))
            );

        var endpointDescriptors = endpointTypes
            .Select(type => ServiceDescriptor.Transient(typeof(ApiEndpoint), type))
            .ToArray();

        services.TryAddEnumerable(endpointDescriptors);
        services.TryAddApiVersioning(endpointTypes);
        services.AddApiValidators(assemblies);

        // Use for API versioning and Swagger generation with Minimal APIs
        services.AddEndpointsApiExplorer();

        return services;
    }

    public static async Task<WebApplication> UseApiEndpointsAsync(
        this WebApplication app,
        Action<ApiEndpointsOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = new ApiEndpointsOptions();
        configure?.Invoke(options);

        IEndpointRouteBuilder endpointRouteBuilder = string.IsNullOrWhiteSpace(options.RoutePrefix)
            ? app
            : app.MapGroup(options.RoutePrefix);

        var endpoints = app.Services
            .GetRequiredService<IEnumerable<ApiEndpoint>>()
            .ToArray();

        var versionsInfo = GetApiVersionsInfo(endpoints);
        var nonVersionedEndpoints = endpoints
            .Where(endpoint => versionsInfo
                .All(metadata => metadata.Endpoint != endpoint))
            .ToArray();

        foreach (var nonVersionedEndpoint in nonVersionedEndpoints)
        {
            MapEndpoint(endpointRouteBuilder, nonVersionedEndpoint, options);
        }

        MapVersionedEndpoints(endpointRouteBuilder, versionsInfo, options);

        await app.ConfigureApiSwaggerAsync();

        return app;
    }

    private static IServiceCollection TryAddApiVersioning(
        this IServiceCollection services,
        IEnumerable<Type> endpointTypes
    )
    {
        var hasApiVersioning = endpointTypes
            .Any(type
                => type.GetCustomAttribute<ApiVersionGroupAttribute>() is not null
                || type.GetCustomAttribute<ApiVersionAttribute>() is not null
            );

        if (!hasApiVersioning)
        {
            return services;
        }

        services
            .AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new(1);
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlPrefixApiVersionReader();
            });

        return services;
    }

    private static ApiVersionInfo[] GetApiVersionsInfo(IEnumerable<ApiEndpoint> endpoints)
    {
        var versions = new List<ApiVersionInfo>();
        foreach (var endpoint in endpoints)
        {
            var endpointType = endpoint.GetType();
            var versionGroupAttribute = endpointType
                .GetCustomAttribute<ApiVersionGroupAttribute>();
            var versionAttribute = endpointType
                .GetCustomAttribute<ApiVersionAttribute>();

            // Non versioned endpoint
            if (versionGroupAttribute is null && versionAttribute is null)
            {
                continue;
            }

            // Misconfigured endpoint
            if (versionGroupAttribute is null || versionAttribute is null)
            {
                throw new InvalidOperationException(
                    $"Endpoint '{endpointType.FullName}' must have either " +
                    $"both '{nameof(ApiVersionGroupAttribute)}' and " +
                    $"'{nameof(ApiVersionAttribute)}' or neither."
                );
            }

            // Disallow minor version
            if (versionAttribute.Versions
                .Any(version => version.MinorVersion > 0))
            {
                throw new InvalidOperationException(
                    $"Endpoint '{endpointType.FullName}' has invalid " +
                    $"API version with minor version specified. " +
                    $"Only major versions are supported."
                );
            }

            var versionToAdd = new ApiVersionInfo
            {
                Endpoint = endpoint,
                EndpointType = endpointType,
                Group = versionGroupAttribute.Group,
                Version = versionAttribute.Versions[0].MajorVersion!.Value,
                IsDeprecated = versionAttribute.Deprecated
            };

            if (versions.Any(info
                => info.Group == versionToAdd.Group
                && info.Version == versionToAdd.Version))
            {
                throw new InvalidOperationException(
                    $"Duplicate API version '{versionToAdd.Version}' " +
                    $"found in group '{versionToAdd.Group}' " +
                    $"for endpoint '{endpointType.FullName}'."
                );
            }

            versions.Add(versionToAdd);
        }

        return [.. versions
            .OrderBy(info => info.Group)
            .ThenBy(info => info.Version)
            .ThenBy(info => info.IsDeprecated)
        ];
    }

    private static void MapVersionedEndpoints(
        IEndpointRouteBuilder endpointRouteBuilder,
        IEnumerable<ApiVersionInfo> versionsInfo,
        ApiEndpointsOptions options
    )
    {
        var versionGroups = versionsInfo
            .GroupBy(metadata => metadata.Group);

        foreach (var versionGroup in versionGroups)
        {
            var versionSetBuilder = endpointRouteBuilder
                .NewApiVersionSet()
                .ReportApiVersions();

            foreach (var version in versionGroup)
            {
                versionSetBuilder = version.IsDeprecated
                    ? versionSetBuilder.HasDeprecatedApiVersion(new(version.Version))
                    : versionSetBuilder.HasApiVersion(new(version.Version));
            }

            var versionSet = versionSetBuilder.Build();
            foreach (var versionInfo in versionGroup)
            {
                var builder = endpointRouteBuilder
                    .MapGroup($"/v{versionInfo.Version}")
                    .WithApiVersionSet(versionSet)
                    .MapToApiVersion(new(versionInfo.Version));

                MapEndpoint(builder, versionInfo.Endpoint, options);
            }
        }
    }

    private static RouteHandlerBuilder MapEndpoint(
        IEndpointRouteBuilder endpointRouteBuilder,
        ApiEndpoint endpoint,
        ApiEndpointsOptions options
    )
    {
        var builder = endpoint.MapEndpoint(endpointRouteBuilder);
        builder.AddEndpointFilter<ApiValidatorFilter>();

        foreach (var filterType in options.EndpointFilterTypes)
        {
            builder.AddEndpointFilterFactory((filterFactoryContext, next) =>
            {
                var filter = (IEndpointFilter)ActivatorUtilities.GetServiceOrCreateInstance(
                    filterFactoryContext.ApplicationServices,
                    filterType
                );

                return invocationContext => filter.InvokeAsync(invocationContext, next);
            });
        }

        foreach (var filterFactory in options.EndpointFilterFactories)
        {
            builder.AddEndpointFilterFactory(filterFactory);
        }

        return builder;
    }
}

using Microsoft.AspNetCore.Http;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.MinimalApis.ApiLogging;

public sealed class EndpointLogScopeOptions
{
    internal const string Key = $"{nameof(EndpointLogScopeOptions)}.Properties";

    public Func<EndpointFilterInvocationContext, IReadOnlyDictionary<string, object?>> PropertiesSelector { get; set; } = DefaultPropertiesSelector;

    private static IReadOnlyDictionary<string, object?> DefaultPropertiesSelector(EndpointFilterInvocationContext context)
        => ImmutableDictionary<string, object?>.Empty;
}

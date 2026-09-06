using Microsoft.AspNetCore.Http;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.MinimalApis.ApiLogging;

public sealed class GlobalLogScopeOptions
{
    public Func<HttpContext, IReadOnlyDictionary<string, object?>> PropertiesSelector { get; set; } = DefaultPropertiesSelector;

    private static IReadOnlyDictionary<string, object?> DefaultPropertiesSelector(HttpContext context)
        => ImmutableDictionary<string, object?>.Empty;
}

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.MinimalApis.ApiIdempotency;

public sealed class ApiIdempotencyOptions
{
    public Func<EndpointFilterInvocationContext, string> KeySuffixFactory { get; set; } = static _ => string.Empty;

    /// <summary>
    /// How long a completed response is replayed for duplicates.
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// In-flight reservation lifetime: set >= the slowest handler so a crashed request
    /// can't block the key forever.
    /// </summary>
    public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromSeconds(30);

    internal static string GetKeyPrefix(EndpointFilterInvocationContext context)
    {
        var request = context.HttpContext.Request;
        return $"ApiIdempotency:{request.Method}:{request.Path}{request.QueryString}:";
    }
}

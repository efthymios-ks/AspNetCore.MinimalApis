using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace Microsoft.AspNetCore.MinimalApis.ApiCaching;

public abstract class ApiCachingOptionsBase
{
    public Func<EndpointFilterInvocationContext, string> KeySuffixFactory { get; set; } = static _ => string.Empty;
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(30);

    internal static string GetKeyPrefix(EndpointFilterInvocationContext context)
    {
        var request = context.HttpContext.Request;
        return $"ApiCaching:{CultureInfo.CurrentCulture.TwoLetterISOLanguageName}:{request.Method}:{request.Path}{request.QueryString}:";
    }
}

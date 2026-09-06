using Asp.Versioning;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.MinimalApis.ApiVersions;

/// <summary>
/// To avoid AddApiExplorer(), separate Swagger documents and <see cref="UrlSegmentApiVersionReader"/>.
/// </summary>
internal sealed class UrlPrefixApiVersionReader : IApiVersionReader
{
    public void AddParameters(IApiVersionParameterDescriptionContext _)
    {
        // Do nothing
    }

    public IReadOnlyList<string> Read(HttpRequest request)
    {
        var path = request.Path.Value;
        if (string.IsNullOrWhiteSpace(path))
        {
            return [];
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var versionSegment = segments.FirstOrDefault(segment
            => segment.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            && segment.Length > 1
        );

        if (versionSegment is null)
        {
            return [];
        }

        var versionAsString = versionSegment[1..];
        return int.TryParse(versionAsString, out _)
            ? [versionAsString]
            : [];
    }
}

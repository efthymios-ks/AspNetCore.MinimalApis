using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;

namespace Microsoft.AspNetCore.MinimalApis.ApiVersions;

internal sealed class ApiVersionInfo
{
    public required ApiEndpoint Endpoint { get; init; }
    public required Type EndpointType { get; init; }
    public required string Group { get; init; }
    public required int Version { get; init; }
    public required bool IsDeprecated { get; init; }
}
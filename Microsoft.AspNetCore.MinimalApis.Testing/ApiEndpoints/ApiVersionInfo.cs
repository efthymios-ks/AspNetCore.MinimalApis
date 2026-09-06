namespace Microsoft.AspNetCore.MinimalApis.Testing.ApiEndpoints;

public sealed class ApiVersionInfo
{
    public required string Group { get; init; }
    public required int Version { get; init; }
    public required bool IsDeprecated { get; init; }
}

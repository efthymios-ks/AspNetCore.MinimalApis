namespace Microsoft.AspNetCore.MinimalApis.Testing.ApiEndpoints;

public sealed class ApiMetadata
{
    private readonly Lazy<string> _route;
    private readonly Lazy<string> _httpMethod;
    private readonly Lazy<IReadOnlyList<object>> _metadata;
    private readonly Lazy<bool> _requiresAuthorization;
    private readonly Lazy<ApiVersionInfo?> _version;
    private readonly Lazy<IReadOnlyList<Attribute>> _classAttributes;

    internal ApiMetadata(
        Func<string> route,
        Func<string> httpMethod,
        Func<IReadOnlyList<object>> metadata,
        Func<bool> requiresAuthorization,
        Func<ApiVersionInfo?> version,
        Func<IReadOnlyList<Attribute>> classAttributes
    )
    {
        _route = new(route);
        _httpMethod = new(httpMethod);
        _metadata = new(metadata);
        _requiresAuthorization = new(requiresAuthorization);
        _version = new(version);
        _classAttributes = new(classAttributes);
    }

    public string Route
        => _route.Value;

    public string HttpMethod
        => _httpMethod.Value;

    public IReadOnlyList<object> Metadata
        => _metadata.Value;

    public bool RequiresAuthorization
        => _requiresAuthorization.Value;

    public ApiVersionInfo? Version
        => _version.Value;

    public IReadOnlyList<Attribute> ClassAttributes
        => _classAttributes.Value;
}

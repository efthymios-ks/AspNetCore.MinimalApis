using Microsoft.OpenApi;

namespace Microsoft.AspNetCore.MinimalApis.ApiSwagger;

public sealed class ApiHeaderAttribute(
    string key,
    string? defaultValue = null,
    bool isRequired = false
    ) : ApiParameterBase
{
    public override ParameterLocation Location { get; } = ParameterLocation.Header;
    public override string Key { get; } = key;
    public override string? DefaultValue { get; } = defaultValue;
    public override bool IsRequired { get; } = isRequired;
}

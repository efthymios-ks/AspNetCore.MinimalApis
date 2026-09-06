using Microsoft.OpenApi;

namespace Microsoft.AspNetCore.MinimalApis.ApiSwagger;

public sealed class ApiQueryAttribute(
    string key,
    string? defaultValue = null,
    bool isRequired = false
    ) : ApiParameterBase
{
    public override ParameterLocation Location { get; } = ParameterLocation.Query;
    public override string Key { get; } = key;
    public override string? DefaultValue { get; } = defaultValue;
    public override bool IsRequired { get; } = isRequired;
}

using Microsoft.OpenApi;

namespace Microsoft.AspNetCore.MinimalApis.ApiSwagger;

public abstract class ApiHeaderDropdownAttributeBase
    : ApiParameterDropdownAttributeBase
{
    public override ParameterLocation Location { get; } = ParameterLocation.Header;
}

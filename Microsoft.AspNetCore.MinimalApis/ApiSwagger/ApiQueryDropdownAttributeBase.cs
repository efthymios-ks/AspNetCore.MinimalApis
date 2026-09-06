using Microsoft.OpenApi;

namespace Microsoft.AspNetCore.MinimalApis.ApiSwagger;

public abstract class ApiQueryDropdownAttributeBase
    : ApiParameterDropdownAttributeBase
{
    public override ParameterLocation Location { get; } = ParameterLocation.Query;
}

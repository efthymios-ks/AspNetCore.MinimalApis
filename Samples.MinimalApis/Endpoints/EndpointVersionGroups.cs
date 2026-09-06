using Microsoft.AspNetCore.MinimalApis.ApiVersions;

namespace Samples.MinimalApis.Endpoints;

public sealed class EndpointVersionGroups
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CreateProductAttribute()
        : ApiVersionGroupAttribute("create-product");
}

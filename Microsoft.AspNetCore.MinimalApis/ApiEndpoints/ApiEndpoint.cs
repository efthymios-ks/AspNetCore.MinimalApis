using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.MinimalApis.ApiEndpoints;

public abstract class ApiEndpoint
{
    public abstract RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder);
}

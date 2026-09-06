using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.MinimalApis.ApiEndpoints;

public sealed class ApiEndpointsOptions
{
    public string? RoutePrefix { get; set; }

    internal ICollection<Type> EndpointFilterTypes { get; } = new HashSet<Type>();
    internal ICollection<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>> EndpointFilterFactories { get; } = [];

    public ApiEndpointsOptions AddGlobalEndpointFilter(Type filterType)
    {
        ArgumentNullException.ThrowIfNull(filterType);

        EndpointFilterTypes.Add(filterType);
        return this;
    }

    public ApiEndpointsOptions AddGlobalEndpointFilter<TFilter>()
        where TFilter : IEndpointFilter
    {
        EndpointFilterTypes.Add(typeof(TFilter));
        return this;
    }

    public ApiEndpointsOptions AddGlobalEndpointFilter(
        Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate> filterFactory
    )
    {
        ArgumentNullException.ThrowIfNull(filterFactory);

        EndpointFilterFactories.Add(filterFactory);
        return this;
    }
}

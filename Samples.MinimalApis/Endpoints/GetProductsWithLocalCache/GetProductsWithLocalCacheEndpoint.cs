using Microsoft.AspNetCore.MinimalApis.ApiCaching.Local;
using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Samples.Shared.Models;
using Samples.Shared.Repositories;

namespace Samples.MinimalApis.Endpoints.GetProductsWithLocalCache;

public sealed class GetProductsWithLocalCacheEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapGet("/products/local-cache", HandleAsync)
            .Produces<Product[]>(StatusCodes.Status200OK)
            .WithTags(EndpointTags.Products)
            .WithLocalApiCaching(options =>
            {
                options.KeySuffixFactory = context
                    => context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                options.CacheDuration = TimeSpan.FromMinutes(5);
            });

    private async Task<IResult> HandleAsync(
        IProductRepository repository,
        ILogger<GetProductsWithLocalCacheEndpoint> logger,
        CancellationToken cancellationToken = default
    )
    {
        // Simulate a slow response
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        var products = await repository.GetProductsAsync(cancellationToken);
        return Results.Ok(products);
    }
}

using Microsoft.AspNetCore.MinimalApis.ApiCaching.Distributed;
using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Samples.Shared.Models;
using Samples.Shared.Repositories;

namespace Samples.MinimalApis.Endpoints.GetProductsWithDistributedCache;

public sealed class GetProductsWithDistributedCacheEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapGet("/products/distributed-cache", HandleAsync)
            .Produces<Product[]>(StatusCodes.Status200OK)
            .WithTags(EndpointTags.Products)
            .WithDistributedApiCaching(options =>
            {
                options.KeySuffixFactory = context
                    => context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                options.CacheDuration = TimeSpan.FromMinutes(10);
            });

    private async Task<IResult> HandleAsync(
        IProductRepository repository,
        ILogger<GetProductsWithDistributedCacheEndpoint> logger,
        CancellationToken cancellationToken = default
    )
    {
        // Simulate a slow response
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        logger.LogInformation("Fetching products with distributed cache");
        var products = await repository.GetProductsAsync(cancellationToken);
        return Results.Ok(products);
    }
}

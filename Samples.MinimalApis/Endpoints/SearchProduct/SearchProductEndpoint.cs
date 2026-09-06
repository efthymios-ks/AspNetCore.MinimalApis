using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Samples.MinimalApis.Endpoints.GetProducts;
using Samples.Shared.Models;
using Samples.Shared.Repositories;

namespace Samples.MinimalApis.Endpoints.SearchProduct;

[LanguageQuery]
public sealed class SearchProductEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapGet($"/products/categories/{{{nameof(SearchProductRequest.CategoryId)}}}", HandleAsync)
            .Produces<Product[]>(StatusCodes.Status200OK)
            .WithTags(EndpointTags.Products);

    private async Task<IResult> HandleAsync(
        [AsParameters] SearchProductRequest request,
        IProductRepository repository,
        CancellationToken cancellationToken = default
    )
    {
        var products = await repository.GetProductsAsync(cancellationToken);
        return Results.Ok(products);
    }
}

using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Microsoft.AspNetCore.MinimalApis.ApiSwagger;
using Samples.Shared.Models;
using Samples.Shared.Repositories;

namespace Samples.MinimalApis.Endpoints.GetProducts;

[ApiHeader("X-AppId")]
[ApiHeader("X-Ip", "127.0.0.1")]
[LanguageHeader]
[LanguageQuery]
public sealed class GetProductsEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapGet("/products", HandleAsync)
            .Produces<Product[]>(StatusCodes.Status200OK)
            .WithTags(EndpointTags.Products);

    private async Task<IResult> HandleAsync(
        IProductRepository repository,
        CancellationToken cancellationToken = default
    )
    {
        var products = await repository.GetProductsAsync(cancellationToken);
        return Results.Ok(products);
    }
}

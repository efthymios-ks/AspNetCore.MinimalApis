using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Microsoft.AspNetCore.MinimalApis.ApiResults;
using Samples.Shared.Models;
using Samples.Shared.Repositories;
using System.Net.Mime;

namespace Samples.MinimalApis.Endpoints.GetProductsXml;

public sealed class GetProductsXmlEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapGet("/products/xml", HandleAsync)
            .Produces<Product[]>(StatusCodes.Status200OK, MediaTypeNames.Application.Xml)
            .WithTags(EndpointTags.Products);

    private async Task<IResult> HandleAsync(
        IProductRepository repository,
        CancellationToken cancellationToken = default
    )
    {
        var products = (await repository
            .GetProductsAsync(cancellationToken))
            .ToArray();
        return Results.Extensions.Xml(products);
    }
}

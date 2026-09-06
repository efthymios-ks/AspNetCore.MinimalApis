using Asp.Versioning;
using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Samples.Shared.Models;
using Samples.Shared.Repositories;
using System.Net.Mime;

namespace Samples.MinimalApis.Endpoints.CreateProduct;

[EndpointVersionGroups.CreateProduct]
[ApiVersion(1, Deprecated = true)]
public sealed class CreateProductEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapPost("/products", HandleAsync)
            .Accepts<CreateProductRequest>(MediaTypeNames.Application.Json)
            .Produces<Product>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .WithTags(EndpointTags.Products);

    private async Task<IResult> HandleAsync(
        CreateProductRequest request,
        IProductRepository repository,
        CancellationToken cancellationToken = default
    )
    {
        var productToCreate = new Product
        {
            Name = request.Name,
            Price = request.Price
        };

        var productCreated = await repository.CreateAsync(productToCreate, cancellationToken);
        return Results.Ok(productCreated);
    }
}

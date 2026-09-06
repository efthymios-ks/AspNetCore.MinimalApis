using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Microsoft.AspNetCore.MinimalApis.ApiIdempotency;
using Microsoft.AspNetCore.MinimalApis.ApiSwagger;
using Samples.Shared.Models;
using Samples.Shared.Repositories;
using System.Net.Mime;

namespace Samples.MinimalApis.Endpoints.CreateProductIdempotent;

// Send the same Idempotency-Key twice: the first call creates the product, the
// second replays the stored response instead of creating a duplicate.
[ApiHeader("Idempotency-Key", "00000000-0000-0000-0000-000000000000")]
public sealed class CreateProductIdempotentEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapPost("/products/idempotent", HandleAsync)
            .Accepts<CreateProductRequest>(MediaTypeNames.Application.Json)
            .Produces<Product>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .WithTags(EndpointTags.Products)
            .WithApiIdempotency();

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

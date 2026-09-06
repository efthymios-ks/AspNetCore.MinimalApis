using FastEndpoints;
using Samples.Shared.Models;
using Samples.Shared.Repositories;

namespace Samples.FastEndpoints.Endpoints.CreateProduct;

public class CreateProductEndpoint(
    IProductRepository repository
    ) : Endpoint<CreateProductRequest, Product>
{
    private readonly IProductRepository _repository = repository;

    public override void Configure()
    {
        Post("/products");
        AllowAnonymous();

        Description(options => options
            .Produces<Product>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
        );

        Version(1, deprecateAt: 2);
    }

    public override async Task HandleAsync(CreateProductRequest req, CancellationToken ct)
    {
        var productToCreate = new Product
        {
            Name = req.Name,
            Price = req.Price
        };

        var productCreated = await _repository.CreateAsync(productToCreate, ct);
        await Send.OkAsync(productCreated, ct);
    }
}

using FastEndpoints;
using Samples.Shared.Models;

namespace Samples.FastEndpoints.Endpoints.CreateProduct;

public sealed class CreateProductEndpointSummary : Summary<CreateProductEndpoint>
{
    public CreateProductEndpointSummary()
    {
        Summary = "Create a new product";
        Description = Summary;

        RequestExamples.Add(new(new CreateProductRequest
        {
            Name = "Smartphone",
            Price = 499.99m
        }, "Simple request"));
    }
}
using Microsoft.AspNetCore.MinimalApis.ApiSwagger;
using Samples.Shared.Models;

namespace Samples.MinimalApis.Endpoints.CreateProductV2;

public sealed class CreateProductEndpointSummary : ApiSummary<CreateProductEndpoint>
{
    public CreateProductEndpointSummary()
    {
        Summary = "Creates a new product";
        Description = Summary;

        AddBodyExample("Simple request", new CreateProductRequest
        {
            Name = "Smartphone",
            Price = 499.99m
        });
    }
}

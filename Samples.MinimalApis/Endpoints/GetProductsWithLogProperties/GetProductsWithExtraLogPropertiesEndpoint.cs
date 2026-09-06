using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Microsoft.AspNetCore.MinimalApis.ApiLogging;
using Samples.Shared.Models;
using Samples.Shared.Repositories;

namespace Samples.MinimalApis.Endpoints.GetProductsWithLogProperties;

public sealed class GetProductsWithExtraLogPropertiesEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapGet("/products/log-properties", HandleAsync)
            .Produces<Product[]>(StatusCodes.Status200OK)
            .WithTags(EndpointTags.Products)
            .WithAdditionalLogProperties(options => options.PropertiesSelector = context =>
            {
                var id = context.GetArgument<int>(0);
                var request = context.HttpContext.Request;
                return new Dictionary<string, object?>
                {
                    ["UserId"] = request.Headers["X-User-Id"].ToString(),
                    ["ProductId"] = id
                };
            });

    private async Task<IResult> HandleAsync(
        int id,
        IProductRepository repository,
        ILogger<GetProductsWithExtraLogPropertiesEndpoint> logger,
        CancellationToken cancellationToken = default
    )
    {
        var products = await repository.GetProductsAsync(cancellationToken);
        return Results.Ok(products);
    }
}

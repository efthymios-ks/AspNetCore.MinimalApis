using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Microsoft.AspNetCore.MinimalApis.Broadcast;
using System.Net.Mime;

namespace Samples.MinimalApis.Endpoints.StreamProductEvents;

public sealed class PublishProductEventEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapPost("/products/events", HandleAsync)
            .Accepts<ProductEvent>(MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status202Accepted)
            .WithTags(EndpointTags.Products);

    private static async Task<IResult> HandleAsync(
        ProductEvent productEvent,
        IBroadcaster broadcaster,
        CancellationToken cancellationToken
    )
    {
        await broadcaster.Broadcast(StreamProductEventsEndpoint.Topic, productEvent, cancellationToken);
        return Results.Accepted();
    }
}
